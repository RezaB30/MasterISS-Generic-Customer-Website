using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RadiusR.API.AddressQueryAdapter
{
    public class AddressServiceAdapter
    {
        private AddressQueryServiceReference.AddressQueryServiceClient ServiceClient { get; set; }

        public AddressServiceAdapter()
        {
            ServiceClient = new AddressQueryServiceReference.AddressQueryServiceClient();
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetProvinces()
        {
            try
            {
                var temp = new AddressQueryServiceReference.AddressQueryRequest()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash()
                };
                var results = ServiceClient.GetProvinces(new AddressQueryServiceReference.AddressQueryRequest()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash()
                });

                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetProvinceDistricts(long provinceId)
        {
            try
            {
                var results = ServiceClient.GetProvinceDistricts(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = provinceId
                });
                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetDistrictRuralRegions(long districtId)
        {
            try
            {
                var results = ServiceClient.GetDistrictRuralRegions(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = districtId
                });
                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetRuralRegionNeighbourhoods(long ruralRegionId)
        {
            try
            {
                var results = ServiceClient.GetRuralRegionNeighbourhoods(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = ruralRegionId
                });
                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetNeighbourhoodStreets(long neighbourhoodId)
        {
            try
            {
                var results = ServiceClient.GetNeighbourhoodStreets(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = neighbourhoodId
                });
                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetStreetBuildings(long streetId)
        {
            try
            {
                var results = ServiceClient.GetStreetBuildings(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = streetId
                });
                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<IEnumerable<ValueNamePair>> GetBuildingApartments(long buildingId)
        {
            try
            {
                var results = ServiceClient.GetBuildingApartments(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = buildingId
                });
                return new RadiusAddress<IEnumerable<ValueNamePair>>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? results.Results.Select(r => new ValueNamePair() { Code = r.Code, Name = r.Name }).ToArray() : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<IEnumerable<ValueNamePair>>(ex);
            }
        }

        public RadiusAddress<AddressDetails> GetApartmentAddress(long apartmentId)
        {
            try
            {
                var results = ServiceClient.GetApartmentAddress(new AddressQueryServiceReference.AddressQueryRequestOflong()
                {
                    Username = AddressAPISettings.AddressAPIUsername,
                    APIHash = GetAPIHash(),
                    RequestValue = apartmentId
                });
                return new RadiusAddress<AddressDetails>()
                {
                    ErrorOccured = !results.IsSuccess,
                    ErrorMessage = results.ErrorMessage,
                    Data = results.IsSuccess ? new AddressDetails() { AddressNo = results.Results.AddressNo, AddressText = results.Results.Address } : null
                };
            }
            catch (Exception ex)
            {
                return GetExceptionResult<AddressDetails>(ex);
            }
        }

        private string GetAPIHash()
        {            
            var seedResult = ServiceClient.GetSeed(AddressAPISettings.AddressAPIUsername);
            if (!seedResult.IsSuccess)
                return null;
            var password = AddressAPISettings.AddressAPIPassword;
            var passwordHash = string.Join(string.Empty, SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password)).Select(b => b.ToString("x2")));
            var result = string.Join(string.Empty, SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(AddressAPISettings.AddressAPIUsername + seedResult.Results + passwordHash)).Select(b => b.ToString("x2")));            
            return result;
        }

        private RadiusAddress<TResult> GetExceptionResult<TResult>(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return new RadiusAddress<TResult>()
            {
                ErrorOccured = true,
                ErrorMessage = ex.Message
            };
        }
    }
}
