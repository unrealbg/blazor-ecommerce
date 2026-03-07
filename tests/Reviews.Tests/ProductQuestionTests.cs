using Reviews.Domain.Questions;

namespace Reviews.Tests;

public sealed class ProductQuestionTests
{
    [Fact]
    public void Create_Should_Fail_When_QuestionTooShort()
    {
        var result = ProductQuestion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            "Short",
            false,
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("reviews.question.too_short", result.Error.Code);
    }

    [Fact]
    public void AddAnswer_Should_AutoApprove_OfficialStaffAnswer_WhenConfigured()
    {
        var question = ProductQuestion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            "Does this support hot swap switches?",
            false,
            DateTime.UtcNow).Value;

        var result = question.AddAnswer(
            customerId: null,
            AnsweredByType.Admin,
            "Support Team",
            "Yes. All sockets are hot swappable.",
            isOfficialAnswer: true,
            autoApprove: true,
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsOfficialAnswer);
        Assert.Equal(Reviews.Domain.Reviews.ModerationStatus.Approved, result.Value.Status);
        Assert.Equal(1, question.AnswerCount);
    }

    [Fact]
    public void HideApprovedAnswer_Should_DecreaseAnswerCount()
    {
        var question = ProductQuestion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            "Does this support Bluetooth?",
            true,
            DateTime.UtcNow).Value;

        var answer = question.AddAnswer(
            Guid.NewGuid(),
            AnsweredByType.Customer,
            "Buyer",
            "Yes, it includes Bluetooth mode.",
            isOfficialAnswer: false,
            autoApprove: true,
            DateTime.UtcNow).Value;

        var result = question.HideAnswer(answer.Id, "Incorrect answer", DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, question.AnswerCount);
    }
}
