using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Headers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GarbageSorter
{
    public class StringTable
    {
        public string[] ColumnNames { get; set; }
        public string[,] Values { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        public MainPage()
        {
            this.InitializeComponent();
            this.initVideo();
        }

        private async void initVideo()
        {
            // Disable all buttons until initialization completes

            try
            {
                status.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for video recording!";

                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                status.Text = "Camera preview succeeded";

            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }

        private async void takePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                takePhoto.IsEnabled = false;
                //var testphoto1 = await StorageFile.GetFileFromPathAsync(@"C:\Users\tkamal\Pictures\photo1.jpg");

                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                takePhoto.IsEnabled = true;
                status.Text = "Take Photo succeeded: " + photoFile.Path;

                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                var byteArray = await ByteConverter(photoStream);
                var base64String = Convert.ToBase64String(byteArray);
                var result = await InvokeRequestResponseService(base64String);

                status.Text = result;
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
            finally
            {
                takePhoto.IsEnabled = true;
            }
        }

        async Task<byte[]> ByteConverter(IRandomAccessStream s)
        {
            var dr = new DataReader(s.GetInputStreamAt(0));
            var bytes = new byte[s.Size];
            await dr.LoadAsync((uint)s.Size);
            dr.ReadBytes(bytes);
            return bytes;
        }


        static async Task<string> InvokeRequestResponseService(string imageBase64)
            
        {
            
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable>() {
                        {
                            "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[] {"Col1"},
                                Values = new string[,] {  { "value" }, }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };
                //scoreRequest["Inputs"]["input1"]["Values"][0,0] = imgBase64;
                scoreRequest.Inputs["input1"].Values[0, 0] = imageBase64;

                const string apiKey = "LzVNDOdF/aS18rtM8Sp/OGdbMgaMDH3TWC+NqhEYCWThWivYuJiA/0j3Eb34zREIfcd1bEalP8T90jzCeAdM1w=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                //client.BaseAddress = new Uri("https://ussouthcentral.services.azureml-int.net/workspaces/4d0bde87ff1c47688121cd6d0c4021d7/services/40bc00f706ae4f9bafc104770701f252/execute?api-version=2.0&details=true");

                // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)


                HttpResponseMessage response = await client.PostAsJsonAsync("https://ussouthcentral.services.azureml-int.net/workspaces/4d0bde87ff1c47688121cd6d0c4021d7/services/40bc00f706ae4f9bafc104770701f252/execute?api-version=2.0&details=true", scoreRequest).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return result;
                    
                }
                else
                {
                    //Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    //Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                    //Console.WriteLine(responseContent);
                }
            }
        }


    }

    
}
