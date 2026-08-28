using System.Text.Encodings.Web;
using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.ContactUs.SubmitContact;

public sealed class SubmitContactService(
    IContactUsRepository contactRepository,
    IContactUsConfigurationRepository contactUsRepository,
    IEmailTemplateRenderer templateRenderer,
    IEmailNotificationService emailService,
    IConfiguration configuration,
    ILogger<SubmitContactService> logger) : ISubmitContactService
{
    public async Task<ResponseResult> ExecuteAsync(SubmitContactRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        logger.LogInformation("Contact submission received. {request}", request);

        try
        {
            var recipient = await contactUsRepository.GetActiveAsync(cancellationToken);
            if (recipient is null)
            {
                logger.LogWarning("Contact submission rejected because no active contact recipient is configured.");
                return response.Fail("Something went wrong, we are working on resolving the issue.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            var contact = FashionStore.Domain.Entities.ContactUs.Create(
                request.Name, request.Email, request.Phone, request.Subject, request.Message);
            await contactRepository.AddAsync(contact, cancellationToken);
            await contactRepository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Contact submission {ContactId} persisted successfully.", contact.Id);

            var appName = configuration["AppSettings:AppName"] ?? "MaisonDeLola";
            var tokens = new Dictionary<string, string>
            {
                ["appName"] = Encode(appName), ["name"] = Encode(contact.Name),
                ["email"] = Encode(contact.Email), ["phone"] = Encode(contact.Phone),
                ["subject"] = Encode(contact.Subject),
                ["message"] = Encode(contact.Message).Replace("\r\n", "<br>").Replace("\n", "<br>"),
                ["year"] = DateTime.UtcNow.Year.ToString()
            };

            var recipientBody = await templateRenderer.RenderAsync(EmailNotificationTypeEnum.ContactRecipient, tokens);
            var customerBody = await templateRenderer.RenderAsync(EmailNotificationTypeEnum.ContactCustomer, tokens);
            await emailService.QueueEmailAsync(new EmailNotification { To = [recipient.ContactEmail], ReplyTo = contact.Email, Subject = $"Contact Message – {contact.Name}", Body = recipientBody }, cancellationToken);
            logger.LogInformation("Recipient notification queued for contact submission {ContactId}.", contact.Id);

            await emailService.QueueEmailAsync(new EmailNotification { To = [contact.Email], Subject = $"We’ve Received Your Message – {appName}", Body = customerBody }, cancellationToken);
            logger.LogInformation("Customer confirmation queued for contact submission {ContactId}.", contact.Id);

            response = response.Success("Your message has been received.");
            response.StatusCode = ResponseCodes.ACCEPTED;
            logger.LogInformation("Contact submission {ContactId} completed successfully.", contact.Id);
            return response;
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Contact submission validation failed for field {Field}.", exception.ParamName);
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Contact submission failed unexpectedly.");
            throw;
        }
    }

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}
