using ECommerce.Shared.Application;
using FluentValidation;
using MediatR;

namespace ECommerce.Tests.Unit.Shared;

public record TestVbRequest(string Name) : IRequest<string>;

public class ValidationBehaviorTests
{
    private sealed class PassingValidator : AbstractValidator<TestVbRequest>
    {
        // No rules — always passes
    }

    private sealed class FailingValidator : AbstractValidator<TestVbRequest>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidators()
    {
        var behavior = new ValidationBehavior<TestVbRequest, string>([]);
        var request = new TestVbRequest("test");

        var result = await behavior.Handle(request, ct => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        var behavior = new ValidationBehavior<TestVbRequest, string>([new PassingValidator()]);
        var request = new TestVbRequest("test");

        var result = await behavior.Handle(request, ct => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        var behavior = new ValidationBehavior<TestVbRequest, string>([new FailingValidator()]);
        var request = new TestVbRequest("");

        Func<Task> act = () => behavior.Handle(request, ct => Task.FromResult("ok"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
