namespace CSharpFunctionalExtensions.Asp
{
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;
    using Microsoft.AspNetCore.Mvc;


    public class CSharpFunctionalBase : ControllerBase
    {

        public ActionResult<T> MapToOkObjectResult<T>(Result<T, ErrorList> result)
        {
            if (result.IsSuccess)
            {
                return base.Ok(result.Value);
            }

            var error = result.Error[0];
            return ConvertToHttpError<T>(error);
        }

        public ActionResult<T> MapToCreatedResult<T>(string location, Result<T, ErrorList> result)
        {
            if (result.IsSuccess)
            {
                return base.Created(location, result.Value);
            }

            var error = result.Error[0];
            return ConvertToHttpError<T>(error);
        }

        private ActionResult<T> ConvertToHttpError<T>(Error error) =>
            error switch
            {
                Errors.NotFound => (ActionResult<T>)base.NotFound(error),
                Errors.Validation => (ActionResult<T>)base.BadRequest(error),
                Errors.Conflict => (ActionResult<T>)base.Conflict(error),
                _ => throw new NotImplementedException($"Unknown error {error.Code}"),
            };
    }
}
