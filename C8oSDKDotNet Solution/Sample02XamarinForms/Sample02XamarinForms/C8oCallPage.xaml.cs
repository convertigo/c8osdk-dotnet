using C8oControls;
using Convertigo.SDK;
using Convertigo.SDK.FullSync;
using Convertigo.SDK.Listeners;
using Convertigo.SDK.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sample02XamarinForms
{
    public partial class C8oCallPage : ContentPage
    {

        private static readonly int NUMBER_OF_VARIABLES = 5;
        private AutoCompleteEntry[][] variablesEntries;

        private C8o c8o;
        private C8oJsonResponseListener c8oJsonResponseListener;

        public C8oCallPage()
        {
            InitializeComponent();

            //*** UI ***//

            this.requestableEntry.Init("requestable");

            this.variablesEntries = new AutoCompleteEntry[NUMBER_OF_VARIABLES][];
            for (int i = 0; i < NUMBER_OF_VARIABLES; i++)
            {
                this.variablesEntries[i] = new AutoCompleteEntry[2];
                this.variablesEntries[i][0] = new AutoCompleteEntry();
                variablesEntries[i][0].HorizontalOptions = LayoutOptions.FillAndExpand;
                variablesEntries[i][0].Init("varKey_" + i);
                this.variablesEntries[i][1] = new AutoCompleteEntry();
                variablesEntries[i][1].HorizontalOptions = LayoutOptions.FillAndExpand;
                variablesEntries[i][1].Init("varValue_" + i);

                StackLayout variableKeyValuePair = new StackLayout();
                variableKeyValuePair.Orientation = StackOrientation.Horizontal;
                variableKeyValuePair.Children.Add(this.variablesEntries[i][0]);
                variableKeyValuePair.Children.Add(this.variablesEntries[i][1]);
                this.variables.Children.Add(variableKeyValuePair);
            }

            // TMP
            //this.requestableEntry.Text = "fs://.view";
            //this.variablesEntries[0][0].Text = "view";
            //this.variablesEntries[0][1].Text = "view01";
            //this.variablesEntries[1][0].Text = "ddoc";
            //this.variablesEntries[1][1].Text = "design";
        }

        public void Init(FullSyncInterface fullSyncInterface, FileReader fileReader)
        {
            //*** C8o objects ***//

            // Settings
            String endpoint = "http://192.168.100.86:18080/convertigo/projects/TestClientSDK";
            C8oSettings c8oSettings = new C8oSettings();
            // c8oSettings.SetFullSyncInterface(fullSyncInterface);
            c8oSettings.SetDefaultFullSyncDatabaseName("testclientsdk_fullsync");
            c8oSettings.SetTimeout(5000);

            // Listeners
            C8oExceptionListener c8oExceptionListener = new C8oExceptionListener((exception, requestaParameters) =>
            {
                String errorMsg = exception.Message;
                Exception inner = exception.InnerException;
                while (inner != null)
                {
                    errorMsg = errorMsg + "\n-->" + inner.Message;
                    inner = inner.InnerException;
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.responseLabel.Text = errorMsg;
                });
            });

            this.c8oJsonResponseListener = new C8oJsonResponseListener((jsonResponse, parameters) =>
            {
                String str = jsonResponse.ToString();
                Debug.WriteLine(str);
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.responseLabel.Text = str;
                });

                //Device.BeginInvokeOnMainThread(() =>
                //{
                //    this.counterLabel.Text = "" + counter + " / " + limit;
                //});

                // Checks if the request is a GetDocument fullSync request
                /*if (FullSyncUtils.IsFullSyncRequest(parameters))
                {
                    String sequence = C8oUtils.GetParameterStringValue(parameters, C8o.ENGINE_PARAMETER_SEQUENCE);
                    if (sequence != null && sequence.Equals("get"))
                    {
                        JToken attachments;
                        if (jsonResponse.TryGetValue("_attachments", out attachments))
                        {
                            if (attachments is JObject)
                            {
                                foreach (KeyValuePair<String, JToken> attachment in (attachments as JObject))
                                {
                                    String attachmentName = attachment.Key;
                                    if (attachment.Value is JObject)
                                    {
                                        JObject attachmentInfo = (JObject)attachment.Value;
                                        String contentUrlStr = GetStringValue(attachmentInfo, "content_url");
                                        String contentTypeStr = GetStringValue(attachmentInfo, "content_type");
                                        if (contentUrlStr != null && contentTypeStr != null && contentTypeStr.Equals("image/png"))
                                        {
                                            try
                                            {
                                                // Checks if the URL is valid
                                                String fileProtocol = "file://";
                                                if (contentUrlStr.Length > fileProtocol.Length && contentUrlStr.StartsWith(fileProtocol))
                                                {
                                                    // Finds the file path
                                                    String filePath = contentUrlStr.Substring(fileProtocol.Length);
                                                    filePath = Uri.UnescapeDataString(filePath);

                                                    byte[] imageBytes = fileReader.ReadFile(filePath);
                                                    ImageSource imageSource = ImageSource.FromStream(() =>
                                                    {
                                                        return new System.IO.MemoryStream(imageBytes);
                                                    });
                                                    Image image = new Image { Aspect = Aspect.AspectFit };
                                                    image.Source = imageSource;
                                                    response.Children.Add(image);

                                                    //Label testLabel = new Label();
                                                    //testLabel.Text = "TOTOTOTOTO";
                                                    //response.Children.Add(testLabel);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                String bp = "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }*/

            });

            this.c8o = new C8o(endpoint, c8oSettings, c8oExceptionListener);

        }

        private static String GetStringValue(JObject jObject, String key)
        {
            JToken value;
            if (jObject.TryGetValue(key, out value) && value is JValue && (value as JValue).Value is String)
            {
                return (value as JValue).Value as String;
            }
            return null;
        }

        void OnCallButtonClicked(object sender, EventArgs args)
        {
            this.requestableEntry.AddToHistory(this.requestableEntry.Text);
            for (int i = 0; i < NUMBER_OF_VARIABLES; i++)
            {
                this.variablesEntries[i][0].AddToHistory(this.variablesEntries[i][0].Text);
                this.variablesEntries[i][1].AddToHistory(this.variablesEntries[i][1].Text);
            }

            Dictionary<String, Object> parameters = new Dictionary<String, Object>();
            for (int i = 0; i < NUMBER_OF_VARIABLES; i++)
            {
                String key = this.variablesEntries[i][0].Text;
                String value = this.variablesEntries[i][1].Text;
                if (key != null && !key.Equals("") && value != null && !value.Equals(""))
                {
                    parameters.Add(this.variablesEntries[i][0].Text, this.variablesEntries[i][1].Text);
                }
            }

            try
            {
                String requestable = this.requestableEntry.Text;
                if (requestable != null && !requestable.Equals(""))
                {
                    /*C8oXmlResponseListener rl = new C8oXmlResponseListener((xmlResponse, requestParameters) =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            String str = (counter++) + " null";
                            if (xmlResponse != null)
                            {
                                str = xmlResponse.ToString();;
                            }
                            this.responseLabel.Text = str;
                        });
                    });*/

                    //Task task = new Task(() =>
                    //{
                        if (requestable.EndsWith("replicate_pullERROR"))
                        {
                            this.c8o.Call(requestable, parameters, null);
                        }
                        else
                        {
                            this.c8o.Call(requestable, parameters, this.c8oJsonResponseListener);
                        }
                    //});
                    //task.Start();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("0000000000000000000");
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine("0000000000000000000");
            }
        }
    }
}
