using BVGF.Connection;
using BVGF.Model;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace BVGF.Pages;
public partial class homePage : ContentPage
{
    private ObservableCollection<MstMember> _members = new ObservableCollection<MstMember>();
    private readonly ApiService _apiService;
    private ObservableCollection<mstCategary> _categories = new ObservableCollection<mstCategary>();
    private Object currentEditingContact;
    private bool isListening = false;
    private int _adInsertInterval = 3;
    private List<AdsEntity> _ads;
    private ObservableCollection<ListItem> _listItems;
    public int TotalRecords => _members?.Count ?? 0;
    

    private readonly ISpeechToText _speechToText;
    public ObservableCollection<MstMember> Members => _members;
    public homePage(ISpeechToText speechToText)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = this;
        _listItems = new ObservableCollection<ListItem>();
        memberCollectionView.ItemsSource = _listItems;
        _apiService = new ApiService();
        LoadCategoriesAsync();
        RecordCountLabel.Text = "Record : 0";
        _members.Clear();
        _speechToText = speechToText;
        _ads = new List<AdsEntity>();
        LoadAdsAsync();
    }

    private async Task LoadAdsAsync()
    {
        try
        {
            _ads = await _apiService.GetAdsAsync();
            Console.WriteLine($"Loaded {_ads.Count} ads");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading ads: {ex.Message}");
            _ads = new List<AdsEntity>();
        }
    }

    private void MergeAdsWithMembers()
    {
        _listItems.Clear();

        if (_members.Count == 0)
        {
            return;
        }

        if (_ads == null || _ads.Count == 0)
        {
            foreach (var member in _members)
            {
                _listItems.Add(new MemberItem(member));
            }
            return;
        }

        if (_members.Count <= 3)
        {
            foreach (var member in _members)
            {
                _listItems.Add(new MemberItem(member));
            }

            _listItems.Add(new AdItem(_ads[0]));
            return;
        }

        int memberIndex = 0;
        int adIndex = 0;

        while (memberIndex < _members.Count)
        {
            for (int i = 0; i < _adInsertInterval && memberIndex < _members.Count; i++)
            {
                _listItems.Add(new MemberItem(_members[memberIndex]));
                memberIndex++;
            }

            if (memberIndex < _members.Count && _ads.Count > 0)
            {
                _listItems.Add(new AdItem(_ads[adIndex % _ads.Count]));
                adIndex++;
            }
        }
    }

    private async void OnCompanyMicClicked(object sender, EventArgs e)
    {
        await StartSpeechToText(CompanyEntry, CompanyMicButton, "Company");
    }

    private async void OnNameMicClicked(object sender, EventArgs e)
    {
        await StartSpeechToText(NameEntry, NameMicButton, "Name");
    }

    private async void OnCityMicClicked(object sender, EventArgs e)
    {
        await StartSpeechToText(CityEntry, CityMicButton, "City");
    }

    private async void OnMobileMicClicked(object sender, EventArgs e)
    {
        await StartSpeechToText(MobileEntry, MobileMicButton, "Mobile", isMobile: true);
    }

    private async Task StartSpeechToText(Entry targetEntry, Button micButton, string fieldName, bool isMobile = false)
    {
        try
        {
            if (isListening)
            {
                await DisplayAlert("Info", "Already listening. Please wait...", "OK");
                return;
            }

            if (targetEntry == null)
            {
                await DisplayAlert("Error", "Entry field is null!", "OK");
                return;
            }

            if (_speechToText == null)
            {
                await DisplayAlert("Error", "Speech service not initialized!", "OK");
                return;
            }

            var granted = await _speechToText.RequestPermissions(CancellationToken.None);
            if (!granted)
            {
                await DisplayAlert("Permission Error", "Microphone permission denied!", "OK");
                ResetMicButton(micButton);
                return;
            }

            await ShowSpeechUI(fieldName, micButton);
            await Task.Delay(500);

            var options = new SpeechToTextOptions
            {
                Culture = CultureInfo.GetCultureInfo("en-US"),
                ShouldReportPartialResults = true
            };

            EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs> updatedHandler = null;
            EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> completedHandler = null;

            updatedHandler = (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var text = e.RecognitionResult;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            text = text.Trim();
                            if (isMobile)
                                text = CleanMobileNumber(text);

                            targetEntry.Text = text;

                            UpdateSpeechStatus($"Listening... \"{text.Substring(0, Math.Min(text.Length, 20))}\"");
                        }
                    }
                    catch { }
                });
            };

            completedHandler = (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var finalText = e.RecognitionResult?.Text;
                        if (!string.IsNullOrWhiteSpace(finalText))
                        {
                            finalText = finalText.Trim();
                            if (isMobile)
                                finalText = CleanMobileNumber(finalText);

                            targetEntry.Text = finalText;

                            UpdateSpeechStatus($"✅ Got: \"{finalText.Substring(0, Math.Min(finalText.Length, 30))}\"");

                          
                            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
                            {
                                try
                                {
                                    OnSearchClicked(null, null);  
                                }
                                catch { }

                                return false;
                            });

                            // Hide UI after 2 sec
                            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
                            {
                                HideSpeechUI();
                                return false;
                            });
                        }
                        else
                        {
                            UpdateSpeechStatus("❌ No speech detected");
                            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
                            {
                                HideSpeechUI();
                                return false;
                            });
                        }
                    }
                    catch { }
                });

                _speechToText.RecognitionResultUpdated -= updatedHandler;
                _speechToText.RecognitionResultCompleted -= completedHandler;
            };

            _speechToText.RecognitionResultUpdated += updatedHandler;
            _speechToText.RecognitionResultCompleted += completedHandler;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await _speechToText.StartListenAsync(options, cts.Token);
        }
        catch (TaskCanceledException)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateSpeechStatus("⏱️ Speech timeout");
                Device.StartTimer(TimeSpan.FromSeconds(2), () =>
                {
                    HideSpeechUI();
                    return false;
                });
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateSpeechStatus($"❌ Error: {ex.Message}");
                Device.StartTimer(TimeSpan.FromSeconds(3), () =>
                {
                    HideSpeechUI();
                    return false;
                });
            });
        }
        finally
        {
            ResetMicButton(micButton);
        }
    }

    private async Task ShowSpeechUI(string fieldName, Button micButton)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            isListening = true;

            micButton.IsEnabled = false;

            SpeechStatusFrame.IsVisible = true;
            SpeechStatusIcon.Text = "⚪";
            SpeechStatusIcon.TextColor = Colors.White;
            SpeakNowLabel.Text = $"Speak {fieldName} now...";

            StartPulsingAnimation();

            // Animate frame appearance
            SpeechStatusFrame.Opacity = 0;
            SpeechStatusFrame.FadeTo(1, 300);
        });
    }

    private void UpdateSpeechStatus(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (SpeechStatusFrame.IsVisible)
            {
                SpeakNowLabel.Text = message;
            }
        });
    }

    private async void HideSpeechUI()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            StopPulsingAnimation();
            await SpeechStatusFrame.FadeTo(0, 300);
            SpeechStatusFrame.IsVisible = false;
            SpeechStatusFrame.Opacity = 1;
        });
    }

    private void StartPulsingAnimation()
    {
        Device.StartTimer(TimeSpan.FromMilliseconds(500), () =>
        {
            if (!isListening || !SpeechStatusFrame.IsVisible)
                return false;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (SpeechStatusIcon.Text == "⚪")
                {
                    SpeechStatusIcon.Text = "⚪";
                    await SpeechStatusIcon.ScaleTo(1.2, 250);
                }
                else
                {
                    SpeechStatusIcon.Text = "⚪";
                    await SpeechStatusIcon.ScaleTo(1.0, 250);
                }
            });

            return isListening && SpeechStatusFrame.IsVisible;
        });
    }

    private void StopPulsingAnimation()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SpeechStatusIcon.Scale = 1.0;
            SpeechStatusIcon.Text = "⚪";
        });
    }

    private void ResetMicButton(Button micButton)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            isListening = false;
            micButton.IsEnabled = true;
        });
    }

    private string CleanMobileNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        var cleaned = input.ToLower()
            .Replace("zero", "0")
            .Replace("one", "1")
            .Replace("two", "2")
            .Replace("three", "3")
            .Replace("four", "4")
            .Replace("five", "5")
            .Replace("six", "6")
            .Replace("seven", "7")
            .Replace("eight", "8")
            .Replace("nine", "9")
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "");

        return new string(cleaned.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }


    private void ShowLoading(bool show, string message = "Searching...")
    {
        LoadingOverlay.IsVisible = show;
        LoadingIndicator.IsVisible = show;
        LoadingIndicator.IsRunning = show;
        LoadingText.IsVisible = show;
        LoadingText.Text = message;

        // Disable search button during loading
        SearchButton.IsEnabled = !show;
        SearchButton.Text = show ? "Searching..." : "Search";
    }


    private async void OnDownloadMemberTapped(object sender, EventArgs e)
    {
        try
        {
            var frame = sender as Frame;
            if (frame?.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tapGesture)
            {
                // Check if it's your member object (adjust according to your actual data type)
                if (tapGesture.CommandParameter is object memberData)
                {
                    // Try to get member details based on your actual data structure
                    MstMember member = null;

                    // Option 1: If your data context directly contains MstMember
                    if (memberData is MstMember directMember)
                    {
                        member = directMember;
                    }
                    // Option 2: If your data context has a Member property
                    else if (memberData.GetType().GetProperty("Member")?.GetValue(memberData) is MstMember memberProperty)
                    {
                        member = memberProperty;
                    }
                    // Option 3: If your data context has a IsAd property (like in your original code)
                    else if (memberData.GetType().GetProperty("IsAd")?.GetValue(memberData) is bool isAd && !isAd)
                    {
                        var memberProp = memberData.GetType().GetProperty("Member");
                        if (memberProp != null)
                        {
                            member = memberProp.GetValue(memberData) as MstMember;
                        }
                    }

                    if (member != null)
                    {
                        await DownloadSingleMemberAsPdf(member);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Download failed: " + ex.Message, "OK");
        }
    }
    private async void OnDownloadAllMembersClicked(object sender, EventArgs e)
    {
        try
        {
            if (_members.Count == 0)
            {
                await DisplayAlert("Info", "No members to download.", "OK");
                return;
            }

            ShowLoading(true, "Generating PDF for all members...");

            var pdfDataList = new List<MemberPdfData>();

            // Convert your members to PDF data
            foreach (var item in _members)
            {
                // Check if it's a member (not ad) and get the member data
                MstMember member = null;

                if (item is MstMember directMember)
                {
                    member = directMember;
                }
                else if (item.GetType().GetProperty("Member")?.GetValue(item) is MstMember memberProperty)
                {
                    member = memberProperty;
                }
                else if (item.GetType().GetProperty("IsAd")?.GetValue(item) is bool isAd && !isAd)
                {
                    var memberProp = item.GetType().GetProperty("Member");
                    if (memberProp != null)
                    {
                        member = memberProp.GetValue(item) as MstMember;
                    }
                }

                if (member != null)
                {
                    pdfDataList.Add(new MemberPdfData
                    {
                        Name = member.Name,
                        Company = member.Company,
                        Category = member.CategoryName,
                        City = member.City,
                        Mobile1 = member.Mobile1
                    });
                }
            }

            var pdf = new BVGF.Connection.PDF();
            var pdfBytes = pdf.GenerateAllMembersPdf(pdfDataList);

            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, $"All_Members_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            File.WriteAllBytes(tempFilePath, pdfBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Download All Members",
                File = new ShareFile(tempFilePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "PDF generation failed: " + ex.Message, "OK");
        }
        finally
        {
            ShowLoading(false);
        }
    }
    private async Task DownloadSingleMemberAsPdf(MstMember member)
    {
        try
        {
            ShowLoading(true, "Generating PDF...");

            // Add small delay to show loading
            await Task.Delay(500);

            var pdf = new BVGF.Connection.PDF();
            var pdfBytes = pdf.GenerateMemberPdf(
                name: member.Name ?? "",
                company: member.Company ?? "",
                category: member.CategoryName ?? "",
                city: member.City ?? "",
                mobile1: member.Mobile1 ?? "",
                mobile2: member.Mobile2,
                mobile3: member.Mobile3,
                telephone: member.Telephone,
                email1: member.Email1,
                email2: member.Email2,
                email3: member.Email3,
                address: member.CityAddress
            );

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DisplayAlert("Error", "Failed to generate PDF content", "OK");
                return;
            }

            // Create safe filename
            var safeName = RemoveInvalidChars(member.Name ?? "Member");
            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            File.WriteAllBytes(tempFilePath, pdfBytes);

            // Verify file was created
            if (!File.Exists(tempFilePath))
            {
                await DisplayAlert("Error", "Failed to create PDF file", "OK");
                return;
            }

            // Show share dialog
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Save {member.Name} Contact",
                File = new ShareFile(tempFilePath)
            });

            await DisplayAlert("Success", "PDF generated successfully! You can now save it to your device.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"PDF generation failed: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"PDF Error: {ex}");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    // Helper method to remove invalid file name characters
    private string RemoveInvalidChars(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return "Member";

        return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }

  
    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BVGF.Pages.history());

    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        try
        {
            // Show loading
            ShowLoading(true, "Searching members...");

            var company = CompanyEntry.Text?.Trim();
            var selectedCategory = CategoryPicker.SelectedItem as mstCategary;
            long? categoryId = selectedCategory?.CategoryID;
            var name = NameEntry.Text?.Trim();
            var city = CityEntry.Text?.Trim();
            var mobile = MobileEntry.Text?.Trim();

            await Task.Delay(300);

            var members = await _apiService.GetMembersAsync(company, categoryId, name, city, mobile);

            _members.Clear();
            foreach (var m in members)
                _members.Add(m);

            MergeAdsWithMembers();

            RecordCountLabel.Text = $"Record : {_members.Count}";

            if (_members.Count == 0)
            {
                await DisplayAlert("Info", "No records found matching your search criteria.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Something went wrong: " + ex.Message, "OK");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        try
        {
            ShowLoading(true, "Resetting...");

            // Clear all search fields
            CompanyEntry.Text = "";
            CategoryPicker.SelectedItem = -1;
            NameEntry.Text = "";
            CityEntry.Text = "";
            MobileEntry.Text = "";

            _members.Clear();
            _listItems.Clear();
            RecordCountLabel.Text = "Record : 0";


            await Task.Delay(200);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to reset: " + ex.Message, "OK");
        }
        finally
        {

            ShowLoading(false);
        }
    }
    private async Task LoadCategoriesAsync()
    {
        var categoryList = await _apiService.GetCategoriesAsync();
        _categories.Clear();
        foreach (var cat in categoryList)
            _categories.Add(cat);

        CategoryPicker.ItemsSource = _categories;
        EditCategoryPicker.ItemsSource = _categories;
    }
    private async void OnContactTapped(object sender, EventArgs e)
    {
        var grid = sender as Grid;

        // Check if BindingContext is MemberItem
        if (grid?.BindingContext is MemberItem memberItem)
        {
            var contact = memberItem.Member;

            if (contact == null)
                return;

            memberCollectionView.IsVisible = false;
            SearchSection.IsVisible = false;
            ContactDetailView.IsVisible = true;
            BackButton.IsVisible = true;
            ContactDetailView.BindingContext = contact;
            await CheckAndShowEditButtonAsync(contact.Mobile1);
            await CheckAndShowPendingApprovalMessage(contact);
        }
        else if (grid?.BindingContext is MstMember contact)
        {
            // Fallback for direct MstMember binding
            if (contact == null)
                return;

            memberCollectionView.IsVisible = false;
            SearchSection.IsVisible = false;
            ContactDetailView.IsVisible = true;
            BackButton.IsVisible = true;
            ContactDetailView.BindingContext = contact;
            await CheckAndShowEditButtonAsync(contact.Mobile1);
            await CheckAndShowPendingApprovalMessage(contact);
        }
    }
    private async Task CheckAndShowPendingApprovalMessage(MstMember contact)
    {
        if (contact.IsEdit == true)
        {
            PendingApprovalMessage.IsVisible = true;
        }
        else
        {
            PendingApprovalMessage.IsVisible = false;
        }
    }
    private async Task CheckAndShowEditButtonAsync(string selectedMobile)
    {
        try
        {
            string storedMobile = await SecureStorage.GetAsync("logged_in_mobile");

            EditContactButton.IsVisible = (storedMobile == selectedMobile);
        }
        catch
        {
            EditContactButton.IsVisible = false;
        }
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        // List view दिखाओ
        ContactDetailView.IsVisible = false;
        BackButton.IsVisible = false;
        memberCollectionView.IsVisible = true;
        SearchSection.IsVisible = true;
        //FloatingButtons.IsVisible = true;
    }
    private string FormatPhoneNumber(string rawNumber)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            return string.Empty;

        var digits = new string(rawNumber
            .Where((c, i) => char.IsDigit(c) || (i == 0 && c == '+'))
            .ToArray());

        if (digits.Length < 10)
            return string.Empty;

        return digits;
    }

    private async void OnCallTapped(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null)
            {
                await DisplayAlert("Error", "No contact selected", "OK");
                return;
            }

            if (DeviceInfo.DeviceType == DeviceType.Virtual)
            {
                await DisplayAlert("Info", "Phone calls cannot be made from emulators", "OK");
                return;
            }

            var availableMobiles = new List<(string Label, string Number, string FormattedNumber)>();

            if (!string.IsNullOrWhiteSpace(contact.Mobile1))
                availableMobiles.Add(("Mobile 1", contact.Mobile1, FormatForDisplay(contact.Mobile1)));

            if (!string.IsNullOrWhiteSpace(contact.Mobile2))
                availableMobiles.Add(("Mobile 2", contact.Mobile2, FormatForDisplay(contact.Mobile2)));

            if (!string.IsNullOrWhiteSpace(contact.Mobile3))
                availableMobiles.Add(("Mobile 3", contact.Mobile3, FormatForDisplay(contact.Mobile3)));

            // If no mobile numbers available
            if (availableMobiles.Count == 0)
            {
                await DisplayAlert("Error", "No mobile number available for this contact", "OK");
                return;
            }

            string selectedNumber;

            // If only one mobile number, use it directly
            if (availableMobiles.Count == 1)
            {
                selectedNumber = availableMobiles[0].Number;
            }
            else
            {
                // Create custom buttons for each number
                var buttons = new Dictionary<string, string>();
                foreach (var mobile in availableMobiles)
                {
                    buttons.Add($"{mobile.Label}\n{mobile.FormattedNumber}", mobile.Number);
                }

                // Show enhanced action sheet
                var action = await DisplayActionSheet(
                    "Choose mobile number to call:",
                    "Cancel",
                    null,
                    buttons.Keys.ToArray()
                );

                if (action == null || action == "Cancel")
                    return;

                // Get the selected number
                if (!buttons.TryGetValue(action, out selectedNumber))
                {
                    await DisplayAlert("Error", "Invalid selection", "OK");
                    return;
                }
            }

            // Format and validate the phone number
            var phoneNumber = FormatPhoneNumber(selectedNumber);
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                await DisplayAlert("Error", "Invalid phone number format", "OK");
                return;
            }

            // Check permissions for Android
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Phone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Phone>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Required",
                            "Phone permission is required to make calls", "OK");
                        return;
                    }
                }
            }

            // Make the call
            try
            {
                if (PhoneDialer.Default.IsSupported)
                {
                    PhoneDialer.Default.Open(phoneNumber);
                }
                else
                {
                    await Launcher.OpenAsync($"tel:{phoneNumber}");
                }
            }
            catch (FeatureNotSupportedException)
            {
                // Final fallback
                await Launcher.OpenAsync($"tel:{phoneNumber}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not initiate call: {ex.Message}", "OK");
        }
    }

    private string FormatForDisplay(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // Format for display (add spaces or dashes for better readability)
        var digits = new string(phoneNumber.Where(c => char.IsDigit(c)).ToArray());

        if (digits.Length == 10)
        {
            return $"{digits.Substring(0, 5)} {digits.Substring(5)}";
        }
        else if (digits.Length > 10 && digits.StartsWith("+"))
        {
            return $"{digits.Substring(0, digits.Length - 10)} {digits.Substring(digits.Length - 10, 5)} {digits.Substring(digits.Length - 5)}";
        }

        return phoneNumber;
    }

    //whatsapp start
    private async void OnWhatsAppTapped(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null)
            {
                await DisplayAlert("Error", "No contact selected", "OK");
                return;
            }

            var availableMobiles = new List<(string Label, string Number, string FormattedNumber)>();

            if (!string.IsNullOrWhiteSpace(contact.Mobile1))
                availableMobiles.Add(("Mobile 1", contact.Mobile1, FormatForDisplay(contact.Mobile1)));
            if (!string.IsNullOrWhiteSpace(contact.Mobile2))
                availableMobiles.Add(("Mobile 2", contact.Mobile2, FormatForDisplay(contact.Mobile2)));
            if (!string.IsNullOrWhiteSpace(contact.Mobile3))
                availableMobiles.Add(("Mobile 3", contact.Mobile3, FormatForDisplay(contact.Mobile3)));

            if (availableMobiles.Count == 0)
            {
                await DisplayAlert("Error", "No mobile number available for this contact", "OK");
                return;
            }

            string selectedNumber;

            if (availableMobiles.Count == 1)
            {
                selectedNumber = availableMobiles[0].Number;
            }
            else
            {
                var buttons = new Dictionary<string, string>();
                foreach (var m in availableMobiles)
                {
                    buttons.Add($"{m.Label} : {m.FormattedNumber}", m.Number);
                }

                var action = await DisplayActionSheet("Choose number for WhatsApp:", "Cancel", null, buttons.Keys.ToArray());
                if (string.IsNullOrWhiteSpace(action) || action == "Cancel") return;

                if (!buttons.TryGetValue(action, out selectedNumber))
                    return;
            }

            var isAvailable = await CheckWhatsAppAvailability(selectedNumber);
            if (!isAvailable)
            {
                await DisplayAlert("Info", $"WhatsApp not available on number {FormatForDisplay(selectedNumber)}", "OK");
                return;
            }

            var formattedNumber = FormatPhoneNumberForWhatsApp(selectedNumber);
            await TryOpenWhatsApp(contact, formattedNumber);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not open WhatsApp: " + ex.Message, "OK");
        }
    }

    private async Task<bool> CheckWhatsAppAvailability(string phoneNumber)
    {
        try
        {
            var cleanNumber = phoneNumber.Replace("+", "");

            var testSchemes = new List<string>();

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                testSchemes.Add($"https://wa.me/{cleanNumber}");
                testSchemes.Add($"whatsapp://send?phone={cleanNumber}");
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                testSchemes.Add($"https://wa.me/{cleanNumber}");
                testSchemes.Add($"whatsapp://send?phone={cleanNumber}");
            }
            else
            {
                testSchemes.Add($"https://wa.me/{cleanNumber}");
            }

            foreach (var scheme in testSchemes)
            {
                try
                {
                    var canOpen = await Launcher.CanOpenAsync(scheme);
                    if (canOpen)
                    {
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }


            return false;
        }
        catch
        {
            return false;
        }
    }
    private async Task<bool> TryOpenWhatsApp(MstMember contact, string phoneNumber)
    {
        try
        {
            var message = GenerateShortWhatsAppMessage(contact);
            var encodedMessage = System.Web.HttpUtility.UrlEncode(message);

            if (encodedMessage.Length > 400)
            {
                //message = $"*{contact.Name}*\n📱 {phoneNumber}\n\n📲 BT Address Book";
                message = "";
                encodedMessage = System.Web.HttpUtility.UrlEncode(message);
            }

            var cleanNumber = phoneNumber.Replace("+", "");

            var schemes = new List<string>();

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                schemes.Add($"https://wa.me/{cleanNumber}?text={encodedMessage}");
                schemes.Add($"whatsapp://send?phone={cleanNumber}&text={encodedMessage}");
                schemes.Add($"https://api.whatsapp.com/send?phone={cleanNumber}&text={encodedMessage}");
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                schemes.Add($"https://wa.me/{cleanNumber}?text={encodedMessage}");
                schemes.Add($"whatsapp://send?phone={cleanNumber}&text={encodedMessage}");
            }
            else
            {
                schemes.Add($"https://wa.me/{cleanNumber}?text={encodedMessage}");
            }

            foreach (var scheme in schemes)
            {
                try
                {
                    var canOpen = await Launcher.CanOpenAsync(scheme);
                    if (canOpen)
                    {
                        await Task.Delay(100);
                        await Launcher.OpenAsync(scheme);
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
    private string FormatPhoneNumberForWhatsApp(string rawNumber)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            return string.Empty;

        var digits = new string(rawNumber
            .Where((c, i) => char.IsDigit(c) || (i == 0 && c == '+'))
            .ToArray());

        digits = digits.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (!digits.StartsWith("+"))
        {

            if (digits.StartsWith("0"))
                digits = digits.Substring(1);

            if (digits.Length == 10)
                digits = "+91" + digits;
            else if (digits.Length == 11 && digits.StartsWith("91"))
                digits = "+" + digits;
            else if (!digits.StartsWith("+"))
                digits = "+" + digits;
        }

        return digits;
    }
    private string GenerateWhatsAppMessage(MstMember contact)
    {
        var message = " ";
        //var message = $"*Contact Details*\n\n";

        //message += $"📝 *Name:* {contact.Name}\n";
        //message += $"📱 *Mobile:* {contact.Mobile1}\n";

        //if (!string.IsNullOrEmpty(contact.Mobile2))
        //    message += $"📞 *Alt Mobile:* {contact.Mobile2}\n";

        //if (!string.IsNullOrEmpty(contact.Company))
        //    message += $"🏢 *Company:* {contact.Company}\n";

        //if (!string.IsNullOrEmpty(contact.City))
        //    message += $"📍 *City:* {contact.City}\n";

        //if (!string.IsNullOrEmpty(contact.Name))
        //    message += $"🏷️ *Category:* {contact.Name}\n";

        //message += "\n📲 *Shared from BT Address Book*";

        return message;
    }
    private string GenerateShortWhatsAppMessage(MstMember contact)
    {
        var message = " ";
        //message += $"📝 *Name:* {contact.Name}\n";
        //message += $"📱 *Mobile:* {contact.Mobile1}\n";

        //if (!string.IsNullOrEmpty(contact.Company))
        //    message += $"🏢 *Company:* {contact.Company}\n";

        //if (!string.IsNullOrEmpty(contact.City))
        //    message += $"📍 *City:* {contact.City}\n";

        //message += "\n📲 BT Address Book";

        return message;
    }
    private async Task OpenWhatsAppViaLauncher(MstMember contact, string phoneNumber)
    {
        try
        {
            // Generate message
            var message = GenerateShortWhatsAppMessage(contact);
            var encodedMessage = System.Web.HttpUtility.UrlEncode(message);

            // Keep URI length under control
            if (encodedMessage.Length > 400)
            {
                // message = $"*{contact.Name}*\n📱 {contact.Mobile1}\n\n📲 BT Address Book";
                message = "";
                encodedMessage = System.Web.HttpUtility.UrlEncode(message);
            }

            // Try different WhatsApp URL schemes
            await TryWhatsAppSchemes(phoneNumber, encodedMessage, contact);
        }
        catch (Exception ex)
        {
            // Try alternative approach
            await TryAlternativeWhatsApp(contact, phoneNumber);
        }
    }

    private async Task TryWhatsAppSchemes(string phoneNumber, string encodedMessage, MstMember contact)
    {
        var schemes = new List<string>();

        // Remove + from phone number for some schemes
        var cleanNumber = phoneNumber.Replace("+", "");

        // Different WhatsApp URL schemes to try
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            schemes.Add($"https://wa.me/{cleanNumber}?text={encodedMessage}");
            schemes.Add($"whatsapp://send?phone={cleanNumber}&text={encodedMessage}");
            schemes.Add($"https://api.whatsapp.com/send?phone={cleanNumber}&text={encodedMessage}");
        }
        else if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            schemes.Add($"https://wa.me/{cleanNumber}?text={encodedMessage}");
            schemes.Add($"whatsapp://send?phone={cleanNumber}&text={encodedMessage}");
        }
        else
        {
            schemes.Add($"https://wa.me/{cleanNumber}?text={encodedMessage}");
        }

        // Try each scheme until one works
        foreach (var scheme in schemes)
        {
            try
            {
                var canOpen = await Launcher.CanOpenAsync(scheme);
                if (canOpen)
                {
                    await Task.Delay(100); // Small delay for stability
                    await Launcher.OpenAsync(scheme);
                    return; // Success, exit method
                }
            }
            catch (Exception ex)
            {
                // Continue to next scheme
                continue;
            }
        }

        // If all schemes fail, try alternative
        await TryAlternativeWhatsApp(contact, phoneNumber);
    }

    private async Task TryAlternativeWhatsApp(MstMember contact, string phoneNumber)
    {
        try
        {
            // Try opening WhatsApp without message first
            var schemes = new List<string>
        {
            "whatsapp://",
            "https://wa.me/",
            "https://web.whatsapp.com/"
        };

            foreach (var scheme in schemes)
            {
                try
                {
                    var canOpen = await Launcher.CanOpenAsync(scheme);
                    if (canOpen)
                    {
                        await Launcher.OpenAsync(scheme);

                        // Copy contact info to clipboard for manual sharing
                        await Task.Delay(1000); // Wait a bit
                        var contactInfo = GenerateWhatsAppMessage(contact);
                        await Clipboard.Default.SetTextAsync(contactInfo);

                        await DisplayAlert("Info",
                            "WhatsApp opened. Contact details copied to clipboard - paste in chat.", "OK");
                        return;
                    }
                }
                catch
                {
                    continue;
                }
            }

            // If WhatsApp can't be opened at all
            throw new Exception("WhatsApp not available");
        }
        catch
        {
            // Final fallback - just copy to clipboard
            await Clipboard.Default.SetTextAsync(GenerateWhatsAppMessage(contact));
            await DisplayAlert("Info",
                "WhatsApp not installed. Contact details copied to clipboard.", "OK");
        }
    }
    private async void OnWhatsAppWithOptionsClicked(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null) return;

            // If contact has multiple mobile numbers, let user choose
            var mobileNumbers = new List<string>();
            if (!string.IsNullOrWhiteSpace(contact.Mobile1))
                mobileNumbers.Add(contact.Mobile1);
            if (!string.IsNullOrWhiteSpace(contact.Mobile2))
                mobileNumbers.Add(contact.Mobile2);

            if (mobileNumbers.Count == 0)
            {
                await DisplayAlert("Error", "No mobile numbers available", "OK");
                return;
            }

            if (mobileNumbers.Count == 1)
            {
                // Single number - send directly
                await SendWhatsAppToNumber(contact, mobileNumbers[0]);
            }
            else
            {
                // Multiple numbers - show selection
                var action = await DisplayActionSheet(
                    "Choose mobile number for WhatsApp:",
                    "Cancel",
                    null,
                    $"Mobile 1: {contact.Mobile1}",
                    $"Mobile 2: {contact.Mobile2}"
                );

                if (action != null && action != "Cancel")
                {
                    var selectedNumber = action.Contains("Mobile 1") ? contact.Mobile1 : contact.Mobile2;
                    await SendWhatsAppToNumber(contact, selectedNumber);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"WhatsApp error: {ex.Message}", "OK");
        }
    }

    private async Task SendWhatsAppToNumber(MstMember contact, string phoneNumber)
    {
        var formattedNumber = FormatPhoneNumberForWhatsApp(phoneNumber);
        await OpenWhatsAppViaLauncher(contact, formattedNumber);
    }



    //whatsapp end
    private async void OnShareTapped(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null)
            {
                await DisplayAlert("Error", "No contact selected", "OK");
                return;
            }

            // Generate the contact information to share
            var shareText = GenerateShareContent(contact);

            try
            {
                var request = new ShareTextRequest
                {
                    Text = shareText,
                    Title = $"Contact: {contact.Name}",
                    Subject = $"Contact Details - {contact.Name}"
                };

                await Share.Default.RequestAsync(request);
            }
            catch (FeatureNotSupportedException)
            {
                // Fallback - copy to clipboard
                await Clipboard.Default.SetTextAsync(shareText);
                await DisplayAlert("Info", "Sharing not available. Contact details copied to clipboard.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not share contact: {ex.Message}", "OK");

            // Fallback - copy to clipboard
            try
            {
                var contact = ContactDetailView.BindingContext as MstMember;
                if (contact != null)
                {
                    await Clipboard.Default.SetTextAsync(GenerateShareContent(contact));
                    await DisplayAlert("Info", "Contact details copied to clipboard.", "OK");
                }
            }
            catch
            {
                // Silent fail for clipboard fallback
            }
        }
    }

    private string GenerateShareContent(MstMember contact)
    {
        var content = $"📋 Contact Details\n\n";
        content += $"👤 Name: {contact.Name}\n";

        // Mobile numbers
        if (!string.IsNullOrEmpty(contact.Mobile1))
            content += $"📱 Mobile: {contact.Mobile1}\n";

        if (!string.IsNullOrEmpty(contact.Mobile2))
            content += $"📞 Mobile 2: {contact.Mobile2}\n";

        if (!string.IsNullOrEmpty(contact.Mobile3))
            content += $"📞 Mobile 3: {contact.Mobile3}\n";

        if (!string.IsNullOrEmpty(contact.Telephone))
            content += $"☎️ Telephone: {contact.Telephone}\n";

        // Email addresses
        if (!string.IsNullOrEmpty(contact.Email1))
            content += $"✉️ Email: {contact.Email1}\n";

        if (!string.IsNullOrEmpty(contact.Email2))
            content += $"📧 Email 2: {contact.Email2}\n";

        if (!string.IsNullOrEmpty(contact.Email3))
            content += $"📧 Email 3: {contact.Email3}\n";

        // Address and other details
        if (!string.IsNullOrEmpty(contact.City))
            content += $"📍 City: {contact.City}\n";

        if (!string.IsNullOrEmpty(contact.CityAddress))
            content += $"🏠 Address: {contact.CityAddress}\n";

        if (!string.IsNullOrEmpty(contact.Company))
            content += $"🏢 Company: {contact.Company}\n";

        if (!string.IsNullOrEmpty(contact.CategoryName))
            content += $"🏷️ Category: {contact.CategoryName}\n";

        content += $"\n📲 Shared from BT Address Book";

        return content;
    }

    // Alternative method with file sharing (VCard)
    private async Task OnShareAsVCardTapped(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null)
            {
                await DisplayAlert("Error", "No contact selected", "OK");
                return;
            }

            // Generate VCard content
            var vCardContent = GenerateVCard(contact);

            // Create temporary file
            var fileName = $"{contact.Name.Replace(" ", "_")}_contact.vcf";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllTextAsync(filePath, vCardContent);

            try
            {
                // Share the file
                var request = new ShareFileRequest
                {
                    Title = $"Contact: {contact.Name}",
                    File = new ShareFile(filePath)
                };

                await Share.Default.RequestAsync(request);
            }
            catch (FeatureNotSupportedException)
            {
                // Fallback to text sharing
                OnShareTapped(sender, e);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not share contact file: {ex.Message}", "OK");

            // Fallback to text sharing
            OnShareTapped(sender, e);
        }
    }

    private string GenerateVCard(MstMember contact)
    {
        var vCard = "BEGIN:VCARD\n";
        vCard += "VERSION:3.0\n";
        vCard += $"FN:{contact.Name}\n";
        vCard += $"N:{contact.Name};;;;\n";

        if (!string.IsNullOrEmpty(contact.Mobile1))
            vCard += $"TEL;TYPE=CELL:{contact.Mobile1}\n";

        if (!string.IsNullOrEmpty(contact.Mobile2))
            vCard += $"TEL;TYPE=CELL:{contact.Mobile2}\n";

        if (!string.IsNullOrEmpty(contact.Mobile3))
            vCard += $"TEL;TYPE=CELL:{contact.Mobile3}\n";

        if (!string.IsNullOrEmpty(contact.Telephone))
            vCard += $"TEL;TYPE=WORK:{contact.Telephone}\n";

        if (!string.IsNullOrEmpty(contact.Email1))
            vCard += $"EMAIL:{contact.Email1}\n";

        if (!string.IsNullOrEmpty(contact.Email2))
            vCard += $"EMAIL:{contact.Email2}\n";

        if (!string.IsNullOrEmpty(contact.Email3))
            vCard += $"EMAIL:{contact.Email3}\n";

        if (!string.IsNullOrEmpty(contact.Company))
            vCard += $"ORG:{contact.Company}\n";

        if (!string.IsNullOrEmpty(contact.CityAddress))
            vCard += $"ADR;TYPE=HOME:;;{contact.CityAddress};{contact.City};;;;\n";

        vCard += "END:VCARD";

        return vCard;
    }

    // Enhanced share method with multiple options
    private async Task OnShareWithOptionsClicked(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null) return;

            var action = await DisplayActionSheet(
                "Share Contact As:",
                "Cancel",
                null,
                "Share as Text",
                "Share as Contact File (VCard)",
                "Copy to Clipboard"
            );

            switch (action)
            {
                case "Share as Text":
                    OnShareTapped(sender, e);
                    break;
                case "Share as Contact File (VCard)":
                    await OnShareAsVCardTapped(sender, e);
                    break;
                case "Copy to Clipboard":
                    await Clipboard.Default.SetTextAsync(GenerateShareContent(contact));
                    await DisplayAlert("Success", "Contact details copied to clipboard!", "OK");
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Share error: {ex.Message}", "OK");
        }
    }

    // Custom share popup with more granular control (Updated)
    private async Task OnCustomShareClicked(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null) return;

            var shareOptions = new List<string>
        {
            "📱 WhatsApp",
            "💬 SMS/Text",
            "📧 Email",
            "📋 Copy to Clipboard",
            "📄 Share as Contact File"

        };

            var action = await DisplayActionSheet(
                $"Share {contact.Name}'s contact via:",
                "Cancel",
                null,
                shareOptions.ToArray()
            );

            if (action == null || action == "Cancel") return;

            switch (action)
            {
                case "📱 WhatsApp":
                    await ShareViaWhatsApp(contact);
                    break;
                case "💬 SMS/Text":
                    await ShareViaSMS(contact);
                    break;
                case "📧 Email":
                    await ShareViaEmail(contact);
                    break;
                case "📋 Copy to Clipboard":
                    await ShareViaClipboard(contact);
                    break;
                case "📄 Share as Contact File":
                    await OnShareAsVCardTapped(sender, e);
                    break;

            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Share failed: {ex.Message}", "OK");
        }
    }

    private async Task ShareViaWhatsApp(MstMember contact)
    {
        try
        {
            var message = GenerateShareContent(contact);
            var encodedMessage = System.Web.HttpUtility.UrlEncode(message);

            // Try to open WhatsApp with the message ready to send
            var whatsappUri = $"https://wa.me/?text={encodedMessage}";

            var canOpen = await Launcher.CanOpenAsync(whatsappUri);
            if (canOpen)
            {
                await Launcher.OpenAsync(whatsappUri);
            }
            else
            {
                // Fallback to regular share
                OnShareTapped(null, null);
            }
        }
        catch
        {
            OnShareTapped(null, null);
        }
    }

    private async Task ShareViaSMS(MstMember contact)
    {
        try
        {
            var message = GenerateShareContent(contact);

            try
            {
                if (Sms.Default.IsComposeSupported)
                {
                    var smsMessage = new SmsMessage
                    {
                        Body = message
                    };
                    await Sms.Default.ComposeAsync(smsMessage);
                }
                else
                {
                    throw new FeatureNotSupportedException();
                }
            }
            catch (FeatureNotSupportedException)
            {
                // Fallback to SMS URI
                var encodedMessage = System.Web.HttpUtility.UrlEncode(message);
                await Launcher.OpenAsync($"sms:?body={encodedMessage}");
            }
        }
        catch
        {
            OnShareTapped(null, null);
        }
    }

    private async Task ShareViaEmail(MstMember contact)
    {
        try
        {
            try
            {
                if (Email.Default.IsComposeSupported)
                {
                    var emailMessage = new EmailMessage
                    {
                        Subject = $"Contact Details - {contact.Name}",
                        Body = GenerateShareContent(contact)
                    };
                    await Email.Default.ComposeAsync(emailMessage);
                }
                else
                {
                    throw new FeatureNotSupportedException();
                }
            }
            catch (FeatureNotSupportedException)
            {
                // Fallback to mailto
                var subject = Uri.EscapeDataString($"Contact Details - {contact.Name}");
                var body = Uri.EscapeDataString(GenerateShareContent(contact));
                await Launcher.OpenAsync($"mailto:?subject={subject}&body={body}");
            }
        }
        catch
        {
            OnShareTapped(null, null);
        }
    }

    private async Task ShareViaClipboard(MstMember contact)
    {
        try
        {
            await Clipboard.Default.SetTextAsync(GenerateShareContent(contact));
            await DisplayAlert("Success", "Contact details copied to clipboard!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to copy to clipboard: {ex.Message}", "OK");
        }
    }

    //end share

    private string FormatPhoneNumberForSMS(string rawNumber)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            return string.Empty;

        // Remove all non-digit characters except '+' at start
        var digits = new string(rawNumber
            .Where((c, i) => char.IsDigit(c) || (i == 0 && c == '+'))
            .ToArray());

        // Remove any spaces, hyphens, brackets, etc.
        digits = digits.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        return digits;
    }
    private string GenerateSMSBody(MstMember contact)
    {
        var message = $"Contact Details:\n";
        message += $"Name: {contact.Name}\n";

        if (!string.IsNullOrEmpty(contact.Company))
            message += $"Company: {contact.Company}\n";

        if (!string.IsNullOrEmpty(contact.City))
            message += $"City: {contact.City}\n";

        message += $"Mobile: {contact.Mobile1}\n";

        if (!string.IsNullOrEmpty(contact.Mobile2))
            message += $"Alt Mobile: {contact.Mobile2}\n";

        if (!string.IsNullOrEmpty(contact.Name))
            message += $"Category: {contact.Name}\n";

        message += "\nShared from BT Address Book";

        return message;
    }
    private async Task OpenSMSViaLauncher(MstMember contact, string phoneNumber)
    {
        try
        {
            var body = GenerateShortSMSBody(contact);
            var encodedBody = System.Web.HttpUtility.UrlEncode(body);

            // Keep URI length under control
            if (encodedBody.Length > 500)
            {
                body = $"Contact: {contact.Name}\nMobile: {contact.Mobile1}\n\nFrom: BT Address Book";
                encodedBody = System.Web.HttpUtility.UrlEncode(body);
            }

            string smsUri;

            // Use simple SMS URI - more compatible
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Android SMS intent
                smsUri = $"sms:{phoneNumber}?body={encodedBody}";
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // iOS SMS scheme
                smsUri = $"sms:{phoneNumber}&body={encodedBody}";
            }
            else
            {
                // Generic SMS scheme
                smsUri = $"sms:{phoneNumber}?body={encodedBody}";
            }

            // Use Launcher with proper error handling
            var canOpen = await Launcher.CanOpenAsync(smsUri);
            if (canOpen)
            {
                await Launcher.OpenAsync(smsUri);
            }
            else
            {
                throw new Exception("No SMS app available");
            }
        }
        catch (Exception ex)
        {
            // Try alternative approach
            await TryAlternativeSMS(contact, phoneNumber);
        }
    }
    private async Task TryAlternativeSMS(MstMember contact, string phoneNumber)
    {
        try
        {
            // Try with minimal data
            var simpleUri = $"sms:{phoneNumber}";
            await Launcher.OpenAsync(simpleUri);

            // If successful, copy details to clipboard for manual entry
            await Task.Delay(500); // Small delay
            var contactInfo = GenerateShortSMSBody(contact);
            await Clipboard.Default.SetTextAsync(contactInfo);

            await DisplayAlert("Info", "SMS app opened. Contact details copied to clipboard - paste in message.", "OK");
        }
        catch
        {
            // Final fallback - just copy to clipboard
            await Clipboard.Default.SetTextAsync(GenerateShortSMSBody(contact));
            await DisplayAlert("Info", "Contact details copied to clipboard", "OK");
        }
    }
    private string GenerateShortSMSBody(MstMember contact)
    {
        var message = $"Contact: {contact.Name}\n";
        message += $"Mobile: {contact.Mobile1}\n";

        if (!string.IsNullOrEmpty(contact.Company))
            message += $"Company: {contact.Company}\n";

        if (!string.IsNullOrEmpty(contact.City))
            message += $"City: {contact.City}\n";

        message += "From: BT Address Book";

        return message;
    }

    private async void OnSMSWithOptionsClicked(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null) return;

            // If contact has multiple mobile numbers, let user choose
            var mobileNumbers = new List<string>();
            if (!string.IsNullOrWhiteSpace(contact.Mobile1))
                mobileNumbers.Add(contact.Mobile1);
            if (!string.IsNullOrWhiteSpace(contact.Mobile2))
                mobileNumbers.Add(contact.Mobile2);

            if (mobileNumbers.Count == 0)
            {
                await DisplayAlert("Error", "No mobile numbers available", "OK");
                return;
            }

            if (mobileNumbers.Count == 1)
            {
                await SendSMSToNumber(contact, mobileNumbers[0]);
            }
            else
            {
                var action = await DisplayActionSheet(
                    "Choose mobile number:",
                    "Cancel",
                    null,
                    $"Mobile 1: {contact.Mobile1}",
                    $"Mobile 2: {contact.Mobile2}"
                );

                if (action != null && action != "Cancel")
                {
                    var selectedNumber = action.Contains("Mobile 1") ? contact.Mobile1 : contact.Mobile2;
                    await SendSMSToNumber(contact, selectedNumber);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"SMS error: {ex.Message}", "OK");
        }
    }
    private async Task SendSMSToNumber(MstMember contact, string phoneNumber)
    {
        var formattedNumber = FormatPhoneNumberForSMS(phoneNumber);

        if (Sms.Default.IsComposeSupported)
        {
            var smsMessage = new SmsMessage
            {
                Body = GenerateSMSBody(contact),
                Recipients = new List<string> { formattedNumber }
            };
            await Sms.Default.ComposeAsync(smsMessage);
        }
        else
        {
            await OpenSMSViaLauncher(contact, formattedNumber);
        }
    }
    private async void OnEmailTapped(object sender, EventArgs e)
    {
        try
        {
            var contact = ContactDetailView.BindingContext as MstMember;
            if (contact == null)
            {
                await DisplayAlert("Error", "No contact selected", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(contact.Email1))
            {
                await DisplayAlert("Error", "No email address available for this contact", "OK");
                return;
            }

            if (!Email.Default.IsComposeSupported)
            {
                // Fallback to mailto: URL scheme
                await OpenEmailViaLauncher(contact);
                return;
            }

            var message = new EmailMessage
            {
                // Subject = $"Contact: {contact.Name}",
                Subject = "",
                Body = GenerateEmailBody(contact),
                // BodyFormat = EmailBodyFormat.PlainText, 
                To = new List<string> { contact.Email1 }
            };


            await Email.Default.ComposeAsync(message);
        }
        catch (FeatureNotSupportedException)
        {
            var contact = ContactDetailView.BindingContext as MstMember;

            await OpenEmailViaLauncher(contact);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not send email: {ex.Message}", "OK");
        }
    }

    private string GenerateEmailBody(MstMember contact)
    {
        return $@"Hii {contact.Name},";
    }


    private async Task OpenEmailViaLauncher(MstMember contact)
    {
        try
        {
            // Check if contact has email for fallback too
            if (string.IsNullOrWhiteSpace(contact.Email1))
            {
                await DisplayAlert("Error", "No email address available for this contact", "OK");
                return;
            }

            var subject = Uri.EscapeDataString($"Contact: {contact.Name}");
            var body = Uri.EscapeDataString(GenerateEmailBody(contact));
            var uri = $"mailto:{contact.Email1}?subject={subject}&body={body}";

            await Launcher.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"No email clients available: {ex.Message}", "OK");

            // Final fallback - copy to clipboard
            await Clipboard.Default.SetTextAsync(GenerateEmailBody(contact));
            await DisplayAlert("Info",
                "Contact details copied to clipboard", "OK");
        }
    }
    private async void OnEditContactClicked(object sender, EventArgs e)
    {
        if (ContactDetailView.BindingContext is MstMember contact)
        {
            try
            {
                // Show loading indicator
                LoadingIndicator.IsVisible = true;

                // API call to get latest member data
                var updatedContact = await _apiService.GetMemberDataFromApi(contact.MemberID);

                if (updatedContact != null)
                {
                    currentEditingContact = updatedContact;
                    PopulateEditForm(updatedContact);
                    ShowEditPopup();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to load contact data", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnEditContactClicked: {ex.Message}");
                await DisplayAlert("Error", "Something went wrong while loading contact", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
            }
        }
    }
    private void ShowEditPopup()
    {
        EditContactOverlay.IsVisible = true;
        // Optional: Add fade-in animation
        EditContactOverlay.FadeTo(1, 250);
    }

    private void PopulateEditForm(MemberEditPopup contact)
    {
        try
        {
            // Basic Information
            EditNameEntry.Text = contact.Name ?? string.Empty;
            EditMobile1Entry.Text = contact.Mobile1 ?? string.Empty;
            EditMobile2Entry.Text = contact.Mobile2 ?? string.Empty;
            EditMobile3Entry.Text = contact.Mobile3 ?? string.Empty;
            EditTelephoneEntry.Text = contact.Telephone ?? string.Empty;

            // Email Information
            EditEmail1Entry.Text = contact.Email1 ?? string.Empty;
            EditEmail2Entry.Text = contact.Email2 ?? string.Empty;
            EditEmail3Entry.Text = contact.Email3 ?? string.Empty;

            // Address Information - Separate fields
            EditCityEntry.Text = contact.City ?? string.Empty;
            EditAddressEditor.Text = contact.Address ?? string.Empty;

            // Company Information - Separate fields
            EditCompanyEntry.Text = contact.Company ?? string.Empty;
            EditCompanyCityEntry.Text = contact.CompCity ?? string.Empty;
            EditCompanyAddressEditor.Text = contact.CompAddress ?? string.Empty;

            // Category
            if (!string.IsNullOrEmpty(contact.CategoryName))
            {
                var selectedCategory = _categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(contact.CategoryName, StringComparison.OrdinalIgnoreCase));
                EditCategoryPicker.SelectedItem = selectedCategory;
            }
            else
            {
                EditCategoryPicker.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error populating edit form: {ex.Message}");
        }
    }
    private async void OnSaveEditClicked(object sender, EventArgs e)
    {
        try
        {
            // First cast the currentEditingContact to MstMember
            if (!(currentEditingContact is MemberEditPopup originalContact))
            {
                await DisplayAlert("Error", "Invalid contact data", "OK");
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(EditNameEntry.Text))
            {
                await DisplayAlert("Validation Error", "Name is required.", "OK");
                return;
            }

            // Show loading indicator
            LoadingOverlay.IsVisible = true;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            LoadingText.IsVisible = true;
            LoadingText.TextColor = Colors.Black;
            LoadingText.Text = "Updating contact...";
            var updateBy = await SecureStorage.GetAsync("member_id");
            // Create or update the member object with separate fields
            var member = new MemberEditPopup
            {
                // Preserve original ID
                MemberID = originalContact.MemberID,

                // Update fields from form
                Name = EditNameEntry.Text.Trim(),
                City = string.IsNullOrWhiteSpace(EditCityEntry.Text) ? null : EditCityEntry.Text.Trim(),
                Address = string.IsNullOrWhiteSpace(EditAddressEditor.Text) ? null : EditAddressEditor.Text.Trim(),
                Mobile1 = EditMobile1Entry.Text.Trim(),
                Mobile2 = string.IsNullOrWhiteSpace(EditMobile2Entry.Text) ? null : EditMobile2Entry.Text.Trim(),
                Mobile3 = string.IsNullOrWhiteSpace(EditMobile3Entry.Text) ? null : EditMobile3Entry.Text.Trim(),
                Telephone = string.IsNullOrWhiteSpace(EditTelephoneEntry.Text) ? null : EditTelephoneEntry.Text.Trim(),
                Email1 = EditEmail1Entry.Text.Trim(),
                Email2 = string.IsNullOrWhiteSpace(EditEmail2Entry.Text) ? null : EditEmail2Entry.Text.Trim(),
                Email3 = string.IsNullOrWhiteSpace(EditEmail3Entry.Text) ? null : EditEmail3Entry.Text.Trim(),
                Company = string.IsNullOrWhiteSpace(EditCompanyEntry.Text) ? null : EditCompanyEntry.Text.Trim(),
                CompCity = string.IsNullOrWhiteSpace(EditCompanyCityEntry.Text) ? null : EditCompanyCityEntry.Text.Trim(),
                CompAddress = string.IsNullOrWhiteSpace(EditCompanyAddressEditor.Text) ? null : EditCompanyAddressEditor.Text.Trim(),
                //CategoryName = (EditCategoryPicker.SelectedItem as mstCategary)?.CategoryName,

                // Preserve original values
                DOB = originalContact.DOB,
                CreatedBy = Convert.ToInt32(updateBy),
                UpdatedBy = Convert.ToInt32(updateBy),
                //UpdatedDt = DateTime.Now
            };

            var apiResponse = await _apiService.UpsertMemberAsync(member);

            // Hide loading
            LoadingOverlay.IsVisible = false;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoadingText.IsVisible = false;

            if (apiResponse?.Status == "200" && apiResponse.Data != null)
            {
                // Update the current editing contact with the response data
                currentEditingContact = apiResponse.Data;
                // Update the UI binding context to reflect changes
                ContactDetailView.BindingContext = null;
                ContactDetailView.BindingContext = currentEditingContact;

                // Hide popup
                HideEditPopup();

                // Show success message
                await DisplayAlert("Success", "Contact updated successfully!", "OK");
            }
            else
            {
                await DisplayAlert("Error", apiResponse?.Message ?? "Failed to update contact. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            // Hide loading
            LoadingOverlay.IsVisible = false;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoadingText.IsVisible = false;

            // Show error message
            await DisplayAlert("Error", "Failed to update contact: " + ex.Message, "OK");
        }
    }
    private void HideEditPopup()
    {
        EditContactOverlay.FadeTo(0, 250).ContinueWith(t =>
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                EditContactOverlay.IsVisible = false;
            });
        });
    }
    private void OnCloseEditPopupClicked(object sender, EventArgs e)
    {
        HideEditPopup();
    }
    private void OnCancelEditClicked(object sender, EventArgs e)
    {
        HideEditPopup();
    }
    protected override bool OnBackButtonPressed()
    {
        if (EditContactOverlay.IsVisible)
        {
            HideEditPopup();
            return true;
        }

        if (ContactDetailView.IsVisible)
        {
            OnBackClicked(null, null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private async void OnAdTapped(object sender, EventArgs e)
    {
        try
        {
            var frame = sender as Frame;

            if (frame?.BindingContext is AdItem adItem)
            {
                var ad = adItem.Ad;
                if (ad != null && !string.IsNullOrWhiteSpace(ad.RedirectUrl))
                {
                    await Launcher.OpenAsync(new Uri(ad.RedirectUrl));
                }
            }
            else if (frame?.BindingContext is AdsEntity ad)
            {
                if (ad != null && !string.IsNullOrWhiteSpace(ad.RedirectUrl))
                {
                    await Launcher.OpenAsync(new Uri(ad.RedirectUrl));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening ad URL: {ex.Message}");
            await DisplayAlert("Error", "Could not open advertisement link.", "OK");
        }
    }
}