namespace CSharpFunctionalExtensions.Asp
{
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public class CSharpFunctionalBase : ControllerBase
    {

        public ActionResult<T> MapToOkObjectResult<T>(Result<T, ErrorList> result)
        {
            if (result.IsSuccess)
                return base.Ok(result.Value);

            return ConvertToHttpError<T>(result.Error);
        }

        public ActionResult<T> MapToCreatedResult<T>(string location, Result<T, ErrorList> result)
        {
            if (result.IsSuccess)
                return base.Created(location, result.Value);

            return ConvertToHttpError<T>(result.Error);
        }

        private ActionResult<T> ConvertToHttpError<T>(ErrorList errors)
        {
            var error = errors[0];
            return error switch
            {
                Errors.NotFound => (ActionResult<T>)base.NotFound(error),
                Errors.Validation => ValidationErrors<T>(errors),
                Errors.Conflict => (ActionResult<T>)base.Conflict(error),
                _ => throw new NotImplementedException($"Unknown error {error.Code}"),
            };
        }

        private ActionResult<T> ValidationErrors<T>(ErrorList errors)
        {
            ModelStateDictionary modelState = new();
            foreach (var error in errors)
            {
                if (error is Validation validation)
                {
                    modelState.AddModelError(validation.Code, validation.Message);
                }
            }
            return ValidationProblem(modelState);
        }
    }
}
