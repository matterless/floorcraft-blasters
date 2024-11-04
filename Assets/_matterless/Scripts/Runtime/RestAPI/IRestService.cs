using System;
using Matterless.Rest;

namespace Matterless.Floorcraft
{
    public interface IRestService
    {
        RestController CreateController(string name);

        string GetLookingGlassProtocolFullUrl(string endPoint);

        void UnsecureGet(string url, Action<string> onSuccess,
            Action<RestService.ErrorResponse> onError = null);

        void UnsecurePostJson(string url, string payload, Action<string> onSuccess,
            Action<RestService.ErrorResponse> onError = null);

        void UnsecurePutJson(string url, string payload, Action<string> onSuccess,
            Action<RestService.ErrorResponse> onError = null);

        void UnsecureDelete(string url, string payload, Action<string> onSuccess,
            Action<RestService.ErrorResponse> onError = null);

        void OnErrorResponse(Action onRefreshAuth, Action<RestService.ErrorResponse> onError,
            RestController.RestCallError error);
    }
}