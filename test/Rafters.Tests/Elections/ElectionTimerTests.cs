using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Rafters.Elections;
using Xunit;

namespace Rafters.Tests.Elections
{
    public class ElectionTimerTests
    {
        private const int TestTimeout = 3000;
        private ElectionTimer _sut;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource(TestTimeout);

        public ElectionTimerTests()
        {
            _sut = new ElectionTimer();
        }

        [Fact]
        public async Task WhenResetIsNotCalled_TaskShouldComplete()
        {
            var electionTask = _sut.WaitForNewElectionTermAsync(_cancellationTokenSource.Token);

            var completedTask = await Task.WhenAny(electionTask, Task.Delay(1000));

            completedTask.Should().Be(electionTask);
        }
        
        [Fact]
        public async Task WhenResetIsCalled_TaskShouldNotComplete()
        {
            // Arrange: Start all processes
            var delayTask = _sut.CompletionTask;
            var electionTask = _sut.WaitForNewElectionTermAsync(_cancellationTokenSource.Token);
            var awaitableTask = Task.WhenAny(electionTask, delayTask);

            // Act: Trigger the reset
            _sut.Reset();
            
            // Assert: Verify the result
            _cancellationTokenSource.IsCancellationRequested.Should().BeFalse();
            delayTask.IsCompleted.Should().BeTrue();
            var completedTask = await awaitableTask;
            completedTask.Should().NotBe(electionTask);
        }
    }
}