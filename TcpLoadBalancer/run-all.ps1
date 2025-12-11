# 1. Set configuration
$Configuration = "Release"

# 2. Build all projects in Release mode
Write-Host "Building all projects in $Configuration mode..."
dotnet build ./TcpLoadBalancer.sln -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Exiting."
    exit 1
}

# 3. Run Unit Tests
Write-Host "`nRunning unit tests..."
dotnet test ./LoadBalancer.UnitTests/LoadBalancer.UnitTests.csproj -c $Configuration --no-build --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unit tests failed. Exiting."
    exit 1
}

# 4. Run Performance Tests (BenchmarkDotNet)
Write-Host "`nRunning performance benchmarks..."
dotnet run --project ./LoadBalancer.PerformanceTests/LoadBalancer.PerformanceTests.csproj -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "Performance tests failed. Exiting."
    exit 1
}

Write-Host "`nAll projects built, unit tests passed, and performance tests executed successfully!"
