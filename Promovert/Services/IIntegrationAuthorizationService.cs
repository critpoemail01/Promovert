using Promovert.Models;

namespace Promovert.Services;

public interface IIntegrationAuthorizationService
{
    PublicationAuthorization GetPostAuthorization(UserNotificationSettings? settings, string platform);
    PublicationAuthorization GetEmailAuthorization(UserNotificationSettings? settings);
}

public sealed record PublicationAuthorization(bool IsAuthorized, string Channel, string Detail);
