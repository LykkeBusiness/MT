// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts.Responses
{
    public class MtBackendResponse<T>
    {
        public T Result { get; set; }
        public string ErrorMessage { get; set; }

        public static MtBackendResponse<T> Ok(T result)
        {
            return new MtBackendResponse<T>
            {
                Result = result
            };
        }
        
        public static MtBackendResponse<T> Error(string message)
        {
            return new MtBackendResponse<T>
            {
                ErrorMessage = message
            };
        }
        
        public static implicit operator Task<MtBackendResponse<T>>(MtBackendResponse<T> response)
        {
            return Task.FromResult(response);
        }
    }
}
