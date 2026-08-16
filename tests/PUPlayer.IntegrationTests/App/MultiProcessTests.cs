namespace PUPlayer.IntegrationTests.App;

public sealed class MultiProcessTests
{
    [Fact]
    public async Task TwoInvocations_StayAliveAsDifferentProcesses()
    {
        using var first = TestApp.Start(TestMedia.OneSecondWave());
        using var second = TestApp.Start(TestMedia.OneSecondWave());

        await Task.WhenAll(first.WaitForReady(), second.WaitForReady());

        Assert.NotEqual(first.Id, second.Id);
    }
}
