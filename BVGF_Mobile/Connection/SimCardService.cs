#if ANDROID
using Android;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.Util;
using Microsoft.Maui.Controls;
#endif

#if IOS
using CoreTelephony;
using Foundation;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Connection
{
    public class SimInfo
    {
        public string PhoneNumber { get; set; } = "";
        public string CarrierName { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public int SlotIndex { get; set; }
        public bool IsActive { get; set; }
        public string DisplayName { get; set; } = "";
        public string SimSerialNumber { get; set; } = "";
        public string NetworkType { get; set; } = "";
        public bool HasPhoneNumber => !string.IsNullOrEmpty(PhoneNumber);
    }

    public enum VerificationResult
    {
        Success,
        NumberNotFound,
        NoSimCards,
        PermissionDenied,
        Error
    }

    public class VerificationResponse
    {
        public VerificationResult Result { get; set; }
        public string Message { get; set; } = "";
        public List<string> AvailableNumbers { get; set; } = new List<string>();
        public bool RequiresAlternativeVerification { get; set; }
    }

    public class SimCardService
    {
        private const string LOG_TAG = "SimCardService";
        private List<SimInfo> _cachedSimInfo = null;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public async Task<List<SimInfo>> GetAllSimCardInfoAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedSimInfo != null &&
                DateTime.Now - _lastCacheUpdate < _cacheExpiry)
            {
                return _cachedSimInfo;
            }

            var simList = new List<SimInfo>();

            try
            {
                var hasPermission = await HasPermissionAsync();
                if (!hasPermission)
                {
                    LogDebug("No permission to read phone state");
                    return simList;
                }

#if ANDROID
                simList = await GetAndroidSimInfoAsync();
#elif IOS
                simList = await GetiOSSimInfoAsync();
#endif

                _cachedSimInfo = simList;
                _lastCacheUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                LogDebug($"Error getting SIM info: {ex.Message}");
            }

            return simList;
        }

        public async Task<bool> HasPermissionAsync()
        {
#if ANDROID
            try
            {
                var activity = Platform.CurrentActivity;
                var context = activity?.ApplicationContext ?? Android.App.Application.Context;

                // Check READ_PHONE_STATE
                var hasPhoneState = ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadPhoneState)
                                    == Android.Content.PM.Permission.Granted;

                if (!hasPhoneState)
                {
                    ActivityCompat.RequestPermissions(
                        activity,
                        new string[] { Manifest.Permission.ReadPhoneState },
                        998
                    );
                    await Task.Delay(2000);
                }

                // Check and Request READ_PHONE_NUMBERS on Android 12+
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    var hasPhoneNumbers = ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadPhoneNumbers)
                                         == Android.Content.PM.Permission.Granted;

                    if (!hasPhoneNumbers)
                    {
                        ActivityCompat.RequestPermissions(
                            activity,
                            new string[] { Manifest.Permission.ReadPhoneNumbers },
                            999
                        );
                        await Task.Delay(2000);
                    }

                    // Final check
                    return ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadPhoneState) == Android.Content.PM.Permission.Granted &&
                           ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadPhoneNumbers) == Android.Content.PM.Permission.Granted;
                }
                else
                {
                    return ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadPhoneState) == Android.Content.PM.Permission.Granted;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"[HasPermissionAsync] Exception: {ex.Message}");
                return false;
            }
#else
            // iOS and others
            var status = await Permissions.RequestAsync<Permissions.Phone>();
            return status == PermissionStatus.Granted;
#endif
        }

#if ANDROID
        private async Task<List<SimInfo>> GetAndroidSimInfoAsync()
        {
            var simList = new List<SimInfo>();

            try
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.ApplicationContext
                             ?? Android.App.Application.Context;

                if (context == null)
                {
                    LogDebug("Android context is null");
                    return simList;
                }

                if (!HasRequiredAndroidPermissions(context))
                {
                    LogDebug("Required permissions not granted");
                    return simList;
                }

                var telephonyManager = (TelephonyManager)context.GetSystemService(Context.TelephonyService);
                if (telephonyManager == null)
                {
                    LogDebug("TelephonyManager is null");
                    return simList;
                }

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                {
                    simList = await GetSimInfoUsingSubscriptionManager(context, telephonyManager);
                }

                if (simList.Count == 0)
                {
                    simList = GetSimInfoUsingTelephonyManager(telephonyManager, context);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Android SIM info error: {ex.Message}");
            }

            return simList;
        }

        private bool HasRequiredAndroidPermissions(Context context)
        {
            // Check READ_PHONE_STATE permission
            bool hasPhoneState = ContextCompat.CheckSelfPermission(
                context,
                Android.Manifest.Permission.ReadPhoneState
            ) == Android.Content.PM.Permission.Granted;

            // If Android 12 or above (S = API 31), also check READ_PHONE_NUMBERS
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
            {
                bool hasPhoneNumbers = ContextCompat.CheckSelfPermission(
                    context,
                    Android.Manifest.Permission.ReadPhoneNumbers
                ) == Android.Content.PM.Permission.Granted;

                return hasPhoneState && hasPhoneNumbers;
            }

            // For Android < 12, only READ_PHONE_STATE is required
            return hasPhoneState;
        }


        private async Task<List<SimInfo>> GetSimInfoUsingSubscriptionManager(Context context, TelephonyManager telephonyManager)
        {
            var simList = new List<SimInfo>();

            try
            {
                var subscriptionManager = SubscriptionManager.From(context);
                if (subscriptionManager == null)
                {
                    LogDebug("SubscriptionManager is null");
                    return simList;
                }

                var subscriptions = subscriptionManager.ActiveSubscriptionInfoList ??
                                   new JavaList<SubscriptionInfo>();

                foreach (var sub in subscriptions)
                {
                    try
                    {
                        var phoneNumber = await GetPhoneNumberForSubscription(context, sub, telephonyManager);

                        var simInfo = new SimInfo
                        {
                            SlotIndex = sub.SimSlotIndex,
                            PhoneNumber = phoneNumber,
                            CarrierName = sub.CarrierName?.ToString() ?? "Unknown",
                            CountryCode = sub.CountryIso ?? "",
                            DisplayName = sub.DisplayName?.ToString() ?? $"SIM {sub.SimSlotIndex + 1}",
                            SimSerialNumber = GetIccId(sub),
                            IsActive = true,
                            NetworkType = GetNetworkTypeName(telephonyManager.NetworkType)
                        };

                        simList.Add(simInfo);
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error processing subscription: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"SubscriptionManager error: {ex.Message}");
            }

            return simList;
        }

        private async Task<string> GetPhoneNumberForSubscription(Context context, SubscriptionInfo subscription, TelephonyManager telephonyManager)
        {
            try
            {
                string number = CleanPhoneNumber(subscription.Number);
                if (!string.IsNullOrEmpty(number)) return number;

                number = CleanPhoneNumber(telephonyManager.Line1Number);
                if (!string.IsNullOrEmpty(number)) return number;

                number = await GetPhoneNumberUsingReflection(context, subscription.SimSlotIndex);
                if (!string.IsNullOrEmpty(number)) return number;

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
                {
                    try
                    {
                        var subTelephonyManager = telephonyManager.CreateForSubscriptionId(subscription.SubscriptionId);
                        number = CleanPhoneNumber(subTelephonyManager?.Line1Number);
                        if (!string.IsNullOrEmpty(number)) return number;
                    }
                    catch { }
                }
            }
            catch { }

            return string.Empty;
        }

        private async Task<string> GetPhoneNumberUsingReflection(Context context, int slotIndex)
        {
            try
            {
                var telephonyManager = (TelephonyManager)context.GetSystemService(Context.TelephonyService);
                if (telephonyManager == null) return string.Empty;

                string[] methodNames = { "getLine1NumberForSlot", "getLine1NumberForSubscriber" };

                foreach (var methodName in methodNames)
                {
                    try
                    {
                        var telephonyClass = Java.Lang.Class.ForName("android.telephony.TelephonyManager");
                        var method = telephonyClass.GetMethod(methodName, Java.Lang.Integer.Type);

                        if (method != null)
                        {
                            var result = method.Invoke(telephonyManager, new Java.Lang.Integer(slotIndex));
                            return CleanPhoneNumber(result?.ToString());
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return string.Empty;
        }

        private string GetIccId(SubscriptionInfo subscription)
        {
            try
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                {
                    return subscription.IccId ?? string.Empty;
                }

                var prop = subscription.GetType().GetProperty("IccId") ??
                           subscription.GetType().GetProperty("Iccid");
                return prop?.GetValue(subscription)?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private List<SimInfo> GetSimInfoUsingTelephonyManager(TelephonyManager telephonyManager, Context context)
        {
            var simList = new List<SimInfo>();

            try
            {
                var simInfo = new SimInfo
                {
                    SlotIndex = 0,
                    PhoneNumber = CleanPhoneNumber(telephonyManager.Line1Number),
                    CarrierName = telephonyManager.NetworkOperatorName ?? "Unknown",
                    CountryCode = telephonyManager.NetworkCountryIso ?? "",
                    DisplayName = "SIM 1",
                    SimSerialNumber = telephonyManager.SimSerialNumber ?? "",
                    IsActive = telephonyManager.SimState == SimState.Ready,
                    NetworkType = GetNetworkTypeName(telephonyManager.NetworkType)
                };

                simList.Add(simInfo);
            }
            catch (Exception ex)
            {
                LogDebug($"TelephonyManager fallback error: {ex.Message}");
            }

            return simList;
        }

        private string GetNetworkTypeName(NetworkType networkType)
        {
            return networkType switch
            {
                NetworkType.Gprs => "2G",
                NetworkType.Edge => "2G",
                NetworkType.Umts => "3G",
                NetworkType.Hsdpa => "3G+",
                NetworkType.Hsupa => "3G+",
                NetworkType.Hspa => "3G+",
                NetworkType.Lte => "4G",
                NetworkType.Nr => "5G",
                _ => "Unknown"
            };
        }
#endif

#if IOS
        private async Task<List<SimInfo>> GetiOSSimInfoAsync()
        {
            var simList = new List<SimInfo>();

            try
            {
                var networkInfo = new CTTelephonyNetworkInfo();
                
                if (networkInfo.ServiceSubscriberCellularProviders != null)
                {
                    int slotIndex = 0;
                    foreach (var carrierPair in networkInfo.ServiceSubscriberCellularProviders)
                    {
                        var carrier = carrierPair.Value as CTCarrier;
                        var simInfo = new SimInfo
                        {
                            SlotIndex = slotIndex,
                            PhoneNumber = "",
                            CarrierName = carrier?.CarrierName ?? "Unknown",
                            CountryCode = carrier?.IsoCountryCode ?? "",
                            DisplayName = $"iPhone SIM {slotIndex + 1}",
                            SimSerialNumber = "",
                            IsActive = true,
                            NetworkType = "Unknown"
                        };
                        
                        simList.Add(simInfo);
                        slotIndex++;
                    }
                }
                else if (networkInfo.SubscriberCellularProvider != null)
                {
                    var carrier = networkInfo.SubscriberCellularProvider;
                    var simInfo = new SimInfo
                    {
                        SlotIndex = 0,
                        PhoneNumber = "",
                        CarrierName = carrier.CarrierName ?? "Unknown",
                        CountryCode = carrier.IsoCountryCode ?? "",
                        DisplayName = "iPhone SIM",
                        SimSerialNumber = "",
                        IsActive = true,
                        NetworkType = "Unknown"
                    };
                    
                    simList.Add(simInfo);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"iOS SIM info error: {ex.Message}");
            }

            return simList;
        }
#endif

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return "";

            string cleaned = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d]", "");

            if (cleaned.Length == 0)
                return "";

            if (cleaned.StartsWith("91") && cleaned.Length == 12)
            {
                cleaned = cleaned.Substring(2);
            }
            else if (cleaned.StartsWith("+91"))
            {
                cleaned = cleaned.Substring(3);
            }
            else if (cleaned.StartsWith("1") && cleaned.Length == 11)
            {
                cleaned = cleaned.Substring(1);
            }
            else if (cleaned.Length > 10)
            {
                cleaned = cleaned.Substring(cleaned.Length - 10);
            }

            return cleaned.Length == 10 && cleaned.All(char.IsDigit) ? cleaned : "";
        }

        public async Task<VerificationResponse> VerifyPhoneNumberAsync(string phoneNumber)
        {
            var response = new VerificationResponse();

            try
            {
                var cleanInput = CleanPhoneNumber(phoneNumber);
                if (string.IsNullOrEmpty(cleanInput))
                {
                    response.Result = VerificationResult.Error;
                    response.Message = "Invalid phone number format";
                    return response;
                }

                var hasPermission = await HasPermissionAsync();
                if (!hasPermission)
                {
                    response.Result = VerificationResult.PermissionDenied;
                    response.Message = "Permission denied to access SIM card information";
                    response.RequiresAlternativeVerification = true;
                    return response;
                }

                var simCards = await GetAllSimCardInfoAsync();
                var availableNumbers = simCards
                    .Where(sim => !string.IsNullOrEmpty(sim.PhoneNumber))
                    .Select(sim => sim.PhoneNumber)
                    .ToList();

                response.AvailableNumbers = availableNumbers;

                if (simCards.Count == 0)
                {
                    response.Result = VerificationResult.NoSimCards;
                    response.Message = "No SIM cards detected in device";
                    response.RequiresAlternativeVerification = true;
                    return response;
                }

                if (availableNumbers.Count == 0)
                {
                    response.Result = VerificationResult.NoSimCards;
                    response.Message = "SIM cards detected but phone numbers are not accessible";
                    response.RequiresAlternativeVerification = true;
                    return response;
                }

                bool numberFound = availableNumbers.Any(num => num == cleanInput);
                if (numberFound)
                {
                    response.Result = VerificationResult.Success;
                    response.Message = "Phone number verified successfully against SIM card";
                }
                else
                {
                    response.Result = VerificationResult.NumberNotFound;
                    response.Message = $"Phone number {cleanInput} not found in your device...";
                    response.RequiresAlternativeVerification = true;
                }
            }
            catch (Exception ex)
            {
                response.Result = VerificationResult.Error;
                response.Message = $"Error during verification: {ex.Message}";
                response.RequiresAlternativeVerification = true;
                LogDebug($"Verification error: {ex.Message}");
            }

            return response;
        }

        public async Task<string> GetSimInfoSummaryAsync()
        {
            try
            {
                var simCards = await GetAllSimCardInfoAsync();

                if (simCards.Count == 0)
                    return "No SIM cards found or permission denied.";

                var summary = new StringBuilder("📱 SIM Card Information:\n\n");

                for (int i = 0; i < simCards.Count; i++)
                {
                    var sim = simCards[i];
                    summary.AppendLine($"SIM {i + 1} (Slot {sim.SlotIndex + 1}):");
                    summary.AppendLine($"  📞 Number: {(string.IsNullOrEmpty(sim.PhoneNumber) ? "Not available" : sim.PhoneNumber)}");
                    summary.AppendLine($"  📡 Carrier: {sim.CarrierName}");
                    summary.AppendLine($"  🏷️ Name: {sim.DisplayName}");
                    summary.AppendLine($"  ✅ Active: {(sim.IsActive ? "Yes" : "No")}");
                    summary.AppendLine($"  🌍 Country: {sim.CountryCode}");
                    summary.AppendLine($"  📶 Network: {sim.NetworkType}\n");
                }

                return summary.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting SIM information: {ex.Message}";
            }
        }

        private void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{LOG_TAG}] {message}");
        }
    }
}