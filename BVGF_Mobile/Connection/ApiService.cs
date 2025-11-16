using BVGF.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BVGF.Connection
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        public ApiService()
        {
            try
            {
                // Create handler that bypasses SSL validation for development
                var handler = new HttpClientHandler();

#if MACCATALYST || IOS
                // For macOS/iOS - disable SSL validation for HTTP
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif

                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(AppSettings.BaseApiUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                System.Diagnostics.Debug.WriteLine($"=== API Base URL: {AppSettings.BaseApiUrl} ===");
                System.Diagnostics.Debug.WriteLine($"=== HttpClient Created Successfully ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ERROR in ApiService Constructor: {ex.Message} ===");
                throw;
            }
        
        }
        public async Task<ApiResponse<MstMember>> LoginAsync(string usermobile)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== LOGIN STARTED for mobile: {usermobile} ===");

                var url = $"{Endpoints.Login}?MobileNo={usermobile}";

                System.Diagnostics.Debug.WriteLine($"=== Full URL: {_httpClient.BaseAddress}{url} ===");
                System.Diagnostics.Debug.WriteLine($"=== Making GET request... ===");

                var response = await _httpClient.GetAsync(url);

                System.Diagnostics.Debug.WriteLine($"=== Response received ===");
                System.Diagnostics.Debug.WriteLine($"=== Status Code: {response.StatusCode} ===");
                System.Diagnostics.Debug.WriteLine($"=== Is Success: {response.IsSuccessStatusCode} ===");

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"=== Response Body: {responseBody} ===");

                    var result = JsonSerializer.Deserialize<ApiResponse<MstMember>>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    System.Diagnostics.Debug.WriteLine($"=== Deserialization successful ===");

                    if (result?.Data?.MemberID != null)
                    {
                        var memberId = result.Data.MemberID.ToString();
                        await SecureStorage.SetAsync("member_id", memberId);
                        System.Diagnostics.Debug.WriteLine($"=== Member ID saved: {memberId} ===");
                    }

                    return result;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"=== API Error Response: {errorBody} ===");
                    return null;
                }
            }
            catch (TaskCanceledException tcEx)
            {
                System.Diagnostics.Debug.WriteLine($"=== TIMEOUT ERROR: Request timed out ===");
                System.Diagnostics.Debug.WriteLine($"=== Message: {tcEx.Message} ===");
                throw new Exception("Request timed out. Please check your internet connection.");
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"=== HTTP REQUEST ERROR ===");
                System.Diagnostics.Debug.WriteLine($"=== Message: {httpEx.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== Inner Exception: {httpEx.InnerException?.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== Stack Trace: {httpEx.StackTrace} ===");
                throw new Exception($"Network error: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"=== JSON PARSING ERROR ===");
                System.Diagnostics.Debug.WriteLine($"=== Message: {jsonEx.Message} ===");
                throw new Exception("Failed to parse server response.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERAL ERROR ===");
                System.Diagnostics.Debug.WriteLine($"=== Exception Type: {ex.GetType().Name} ===");
                System.Diagnostics.Debug.WriteLine($"=== Message: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== Stack Trace: {ex.StackTrace} ===");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"=== Inner Exception: {ex.InnerException.Message} ===");
                    System.Diagnostics.Debug.WriteLine($"=== Inner Stack Trace: {ex.InnerException.StackTrace} ===");
                }

                throw;
            }
        }


        public async Task<List<MstMember>> GetMembersAsync(
     string company, long? category, string name, string city, string mobile)
        {
            var queryParams = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(company))
                queryParams.Add("Company", company);

            if (category.HasValue && category.Value > 0)
                queryParams.Add("CatName", category.Value.ToString());

            if (!string.IsNullOrWhiteSpace(name))
                queryParams.Add("Name", name);

            if (!string.IsNullOrWhiteSpace(city))
                queryParams.Add("City", city);

            if (!string.IsNullOrWhiteSpace(mobile))
                queryParams.Add("Mobile1", mobile);

            var queryString = string.Join("&",
                queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            var url = $"{Endpoints.SearchMember}?{queryString}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<MstMember>();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<MemberResponseData>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Data?.Members ?? new List<MstMember>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("API error: " + ex.Message);
                return new List<MstMember>();
            }
        }

        public async Task<List<AdsEntity>> GetAdsAsync()
        {
            var url = "http://195.250.31.98:8070/api/Ads/GetAdsData";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<AdsEntity>();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdsResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Data ?? new List<AdsEntity>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("API error fetching ads: " + ex.Message);
                return new List<AdsEntity>();
            }
        }

       

        public async Task<List<mstCategary>> GetCategoriesAsync()
        {
            try
            {
                var url = $"{Endpoints.CategaryDrp}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<mstCategary>();

                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ApiResponse<List<mstCategary>>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Data ?? new List<mstCategary>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Category API error: " + ex.Message);
                return new List<mstCategary>();
            }
        }

        public async Task<ApiResponse<int>> UpsertMemberAsync(MemberEditPopup member)
        {
            try
            {
                var url = Endpoints.EditMember;
                var json = JsonSerializer.Serialize(member);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResponse<int>
                    {
                        Status = "Failed",
                        Message = $"API request failed with status: {response.StatusCode}",
                        Data = 0
                    };
                }else
                {
                    return new ApiResponse<int>
                    {
                        Status = "200",
                        Message = "Member updated successfully",
                        Data = 1
                    };
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpsertMemberAsync Exception: {ex.Message}");
                return new ApiResponse<int>
                {
                    Status = "Failed",
                    Message = ex.Message,
                    Data = 0
                };
            }
        }
        public async Task<MemberEditPopup> GetMemberDataFromApi(long memberId)
        {
            try
            {
                var url = $"{Endpoints.GetMemberDataByMemberId}?MemberId={memberId}";

                 var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<MemberEditPopup>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result.Data;
                }
                else
                {
                    Console.WriteLine($"API call failed with status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request error: {httpEx.Message}");
                return null;
            }
            catch (TaskCanceledException tcEx)
            {
                Console.WriteLine($"Request timeout: {tcEx.Message}");
                return null;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON deserialization error: {jsonEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return null;
            }
        }




    }

}

