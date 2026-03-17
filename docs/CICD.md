# CI/CD 配置指南

本文档描述如何为 TdxDllGenerator 项目配置持续集成和持续部署。

## GitHub Actions 配置

创建 `.github/workflows/dotnet.yml` 文件：

```yaml
name: .NET CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: 设置 .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: 还原依赖
      run: dotnet restore
    
    - name: 构建
      run: dotnet build --no-restore --configuration Release
    
    - name: 运行测试
      run: dotnet test tests/TdxDllGenerator.Tests --no-build --verbosity normal
    
    - name: 生成覆盖率报告
      run: dotnet test tests/TdxDllGenerator.Tests --collect:"XPlat Code Coverage" --no-build
    
    - name: 上传覆盖率报告
      uses: codecov/codecov-action@v3
      with:
        files: tests/TdxDllGenerator.Tests/TestResults/**/coverage.cobertura.xml
```

## Azure DevOps 配置

创建 `azure-pipelines.yml` 文件：

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'
    includePreviewVersions: true

- script: dotnet restore
  displayName: '还原依赖'

- script: dotnet build --no-restore --configuration Release
  displayName: '构建项目'

- script: dotnet test tests/TdxDllGenerator.Tests --no-build --verbosity normal
  displayName: '运行测试'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
    mergeTestResults: true
  condition: always()
```

## 本地 CI 脚本

创建 `scripts/ci.sh`：

```bash
#!/bin/bash
set -e

echo "=== 开始 CI 流程 ==="

# 还原依赖
echo "[1/4] 还原依赖..."
dotnet restore

# 构建
echo "[2/4] 构建项目..."
dotnet build --no-restore --configuration Release

# 运行测试
echo "[3/4] 运行测试..."
dotnet test tests/TdxDllGenerator.Tests --no-build --verbosity normal

# 检查代码格式
echo "[4/4] 检查代码格式..."
dotnet format --verify-no-changes --verbosity diagnostic || true

echo "=== CI 流程完成 ==="
```

## 自动化发布

### 创建发布工作流

```yaml
name: Release

on:
  push:
    tags:
    - 'v*'

jobs:
  release:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: 设置 .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: 发布
      run: dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
    
    - name: 打包
      run: |
        cd src/bin/Release/net8.0/win-x64/publish
        zip -r TdxDllGenerator.zip *
    
    - name: 创建发布
      uses: softprops/action-gh-release@v1
      with:
        files: src/bin/Release/net8.0/win-x64/publish/TdxDllGenerator.zip
```

## 测试矩阵

配置多平台测试：

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
    dotnet: ['8.0.x']
```

## 代码质量检查

集成代码分析工具：

```yaml
- name: 运行代码分析
  run: |
    dotnet tool install --global dotnet-sonarscanner
    dotnet sonarscanner begin /k:project-key /d:sonar.host.url=$SONAR_URL /d:sonar.login=$SONAR_TOKEN
    dotnet build
    dotnet sonarscanner end /d:sonar.login=$SONAR_TOKEN
```

## 最佳实践

1. **并行执行**：使用矩阵构建在多个平台上并行测试
2. **缓存优化**：缓存 NuGet 包以加快构建速度
3. **失败快速**：配置快速失败以节省资源
4. **通知集成**：集成 Slack/Teams 通知构建状态
5. **安全扫描**：集成依赖漏洞扫描
