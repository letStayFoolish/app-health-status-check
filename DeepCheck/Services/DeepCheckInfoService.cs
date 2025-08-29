namespace DeepCheck.Services;

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DeepCheck.DTOs;
using DeepCheck.Interfaces;
using DeepCheck.Repositories;

public class DeepCheckInfoService : IDeepCheckInfoService
{
    private readonly ITestRepository testRepository;
    private readonly IEnumerable<ITest> tests;

    public DeepCheckInfoService(ITestRepository testRepository, IEnumerable<ITest> tests)
    {
        this.testRepository = testRepository;
        this.tests = tests;
    }

    public async Task<DeepCheckInfo> GetDeepCheckInfo(CancellationToken cancellationToken = default)
    {
        var testRuns = await testRepository.GetUptimeTestRunInfoAsync(10, cancellationToken);
        var productVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var fileVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.0";
        return new DeepCheckInfo()
        {
            TestRunDefinitions = tests.Select(t => t.TestDefinition).ToList(),
            AppVersion = new Models.AppVersion(productVersion, fileVersion),
            UptimeTestRunInfo = testRuns
        };
    }
}
