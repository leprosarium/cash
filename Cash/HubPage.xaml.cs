using Cash.Common;
using Cash.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace XmlReaderExtensions
{
    public static class AsyncExtension
    {
        public static async Task<bool> ReadToFollowingAsync(this XmlReader reader, string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0) throw new ArgumentException("localName is empty or null");
            if (namespaceURI == null) throw new ArgumentNullException("namespaceURI");

            localName = reader.NameTable.Add(localName);
            namespaceURI = reader.NameTable.Add(namespaceURI);

            while (await reader.ReadAsync())
                if (reader.NodeType == XmlNodeType.Element && ((object)localName == (object)reader.LocalName) && ((object)namespaceURI == (object)reader.NamespaceURI))
                    return true;
            return false;
        }

        public static async Task<bool> ReadToFollowingAsync(this XmlReader reader, string name)
        {
            name = reader.NameTable.Add(name);
            while (await reader.ReadAsync())
                if (reader.NodeType == XmlNodeType.Element && (object)name == (object)reader.Name)
                    return true;
            return false;
        }
    }
}

namespace Cash
{
    using XmlReaderExtensions;
    using Model.Import2;
    using Windows.Storage.Pickers;
    using Windows.ApplicationModel.Activation;

    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        private FileOpenPickerContinuationEventArgs _filePickerEventArgs = null;
        public FileOpenPickerContinuationEventArgs FilePickerEvent
        {
            get { return _filePickerEventArgs; }
            set
            {
                _filePickerEventArgs = value;
                ContinueFileOpenPicker(_filePickerEventArgs);
            }
        }


        public HubPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroups = await SampleDataSource.GetGroupsAsync();
            this.DefaultViewModel["Groups"] = sampleDataGroups;
            this.defaultViewModel["AccountTags"] = SampleDataSource.GetAccountTags();
            this.defaultViewModel["Currencies"] = SampleDataSource.GetCurrencies();
            this.defaultViewModel["Imported"] = new ObservableCollection<String>();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        /// <summary>
        /// Shows the details of a clicked group in the <see cref="SectionPage"/>.
        /// </summary>
        private void GroupSection_ItemClick(object sender, ItemClickEventArgs e)
        {
            var groupId = ((SampleDataGroup)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(SectionPage), groupId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        /// <summary>
        /// Shows the details of an item clicked on in the <see cref="ItemPage"/>
        /// </summary>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(ItemPage), itemId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async Task ImportFromStream(Stream stream)
        {
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Async = true;
                XmlReader reader = XmlReader.Create(gzip, settings);

                if (!await reader.ReadToFollowingAsync("gnc-v2"))
                    return ;

                importProgress.Maximum = 1;
                importProgress.Value = 0;
                importProgress.Visibility = Visibility.Visible;
                var bookReader = new BookReader();
                bookReader.Progress += Progress;
                var gncParser = reader.Register(bookReader.AsListReader("gnc:book"));
                var books = await reader.ReadAsyncBy(gncParser);

                if (books.Any())
                {
                    //ObservableCollection<string> imported = (ObservableCollection<string>)this.defaultViewModel["Imported"];
                    //imported.Add(string.Format("Accounts: {0}", books[0].Accounts.Count));
                    //imported.Add(string.Format("Transactions: {0}", books[0].Transactions.Count));
                    //foreach (var acc in books[0].Accounts)
                    //    imported.Add(string.Format("{0} {1} {2} {3} {4}", acc.Name, acc.Type, acc.ID, acc.Comm.Space, acc.Comm.ID));

                    Cash.Model.Account root = GnuCash.MakeAccountTree(books[0].Accounts);
                    List<string> flat = new List<string>();
                    string[] ll = new string[] { "1" };
                    string ss = ll.ToString();
                    flat.Add(ss);
                    root.FlatList("", ";", ref flat);
                    this.defaultViewModel["Imported"] = new ObservableCollection<string>(flat);
                    this.defaultViewModel["Imported2"] = root.SubAccounts;
                }
                importProgress.Visibility = Visibility.Collapsed;
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            string folderName = "store";
            string fileName = "Бухгалтерия.gnucash";

            var devices = Windows.Storage.KnownFolders.RemovableDevices;
            var sdCards = await devices.GetFoldersAsync();

            if (sdCards.Count == 0) return;

            var firstCard = sdCards[0];

            StorageFolder notesFolder = await firstCard.GetFolderAsync(folderName);
            await ImportFromStream(await notesFolder.OpenStreamForReadAsync(fileName));
        }
        private void Progress(object sender, ProgressEventArgs e)
        {
            importProgress.Maximum = e.Maximum;
            importProgress.Value = e.Value;
        }

        private void Import2_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".gnucash");
            picker.ContinuationData["Operation"] = "ImportGNUCash";
            picker.PickSingleFileAndContinue();
        }

        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if ((args.ContinuationData["Operation"] as string) == "ImportGNUCash" &&
                args.Files != null &&
                args.Files.Count > 0)
            {
                StorageFile file = args.Files[0];
                await ImportFromStream(await file.OpenStreamForReadAsync());
            }
        }

        private void GroupedListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var a = (Cash.Model.Account)e.ClickedItem;
            this.defaultViewModel["Imported2"] = a.SubAccounts;
        }
 

    }
}
