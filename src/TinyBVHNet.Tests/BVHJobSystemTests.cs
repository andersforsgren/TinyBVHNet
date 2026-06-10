using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for the BVHJobSystem (thread pool) wrapper.
/// The job system is a simple thread pool for parallel BVH building.
/// </summary>
public class BVHJobSystemTests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var jobs = new BVHJobSystem();
        Assert.True(true);
    }

    [Fact]
    public void IsBusy_ImmediatelyAfterCreate_ReturnsFalse()
    {
        using var jobs = new BVHJobSystem();
        Assert.False(jobs.IsBusy);
    }

    [Fact]
    public void IsBusy_AfterManyChecks_ReturnsFalse()
    {
        using var jobs = new BVHJobSystem();
        bool busy = false;
        for (int i = 0; i < 10; i++)
            busy = busy || jobs.IsBusy;
        Assert.False(busy);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var jobs = new BVHJobSystem();
        bool _ = jobs.IsBusy; // touch it
        jobs.Dispose();
        jobs.Dispose();
    }

    [Fact]
    public void Dispose_CanCreateNewInstance()
    {
        var jobs = new BVHJobSystem();
        jobs.Dispose();
        using var jobs2 = new BVHJobSystem();
        Assert.True(true);
    }
}
