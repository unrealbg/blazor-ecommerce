namespace Storefront.Web.Services.Api;

public sealed record StoreSubmitReviewRequest(
    Guid? VariantId,
    string? Title,
    string? Body,
    int Rating);

public sealed record StoreSubmitQuestionRequest(string QuestionText);

public sealed record StoreSubmitAnswerRequest(string AnswerText);
