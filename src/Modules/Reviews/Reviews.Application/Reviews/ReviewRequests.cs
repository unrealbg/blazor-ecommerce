namespace Reviews.Application.Reviews;

public sealed record SubmitReviewRequest(
    Guid? VariantId,
    string? Title,
    string? Body,
    int Rating);

public sealed record SubmitQuestionRequest(string QuestionText);

public sealed record SubmitAnswerRequest(string AnswerText);

public sealed record OfficialAnswerRequest(string DisplayName, string AnswerText);
