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
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppSettings.BaseApiUrl)
            };
        }
        public async Task<ApiResponse<MstMember>> LoginAsync(string usermobile)
        {
            try
            {
                var url = $"{Endpoints.Login}?MobileNo={usermobile}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<MstMember>>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result?.Data?.MemberID != null)
                    {
                        var memberId = result.Data.MemberID.ToString();
                        await SecureStorage.SetAsync("member_id", memberId);
                    }

                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login API Exception: " + ex.Message);
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

