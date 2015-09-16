using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace C8oControls
{
    public class AutoCompleteEntry : StackLayout
    {

        //*** Constants ***// 

        /// <summary>
        /// The maximum of values saved in history.
        /// </summary>
        private static readonly int HISTORY_LIMIT = 5;

        //*** Atributes ***//
        
        /// <summary>
        /// Indicates if it is initialized or not.
        /// </summary>
        private Boolean initialized;
        /// <summary>
        /// The history.
        /// </summary>
        private List<String> history;
        /// <summary>
        /// The property identifierr in which history is saved.
        /// </summary>
        private String historyIdentifier;

        //*** UI ***//

        /// <summary>
        /// 
        /// </summary>
        private Entry entry;
        /// <summary>
        /// 
        /// </summary>
        private ListView listView;
        /// <summary>
        /// 
        /// </summary>
        public String Text
        {
            get { return this.entry.Text; }
            set { this.entry.Text = value; }
        }

        //*** Constructors ***//

        public AutoCompleteEntry()
            : base()
        {
            this.history = new List<String>();
            this.entry = new Entry();
            this.listView = new ListView();
            this.listView.IsVisible = false;
            this.listView.RowHeight = 35;

            this.Children.Add(this.entry);
            this.Children.Add(this.listView);

            //*** Entry events ***//

            // Display the history
            this.entry.Focused += (sender, focusEventArgs) =>
            {
                this.listView.IsVisible = true;
                this.UpdateHistoryInView(this.entry.Text);
            };
            // Hide the history
            this.entry.Unfocused += (sender, focusEventArgs) =>
            {
                this.listView.IsVisible = false;
            };
            // Update the history
            this.entry.TextChanged += (sender, textChangedEventArgs) =>
            {
                if (this.listView.IsVisible)
                {
                    this.UpdateHistoryInView(this.entry.Text);
                }
            };

            //*** ListView events ***//
            this.listView.ItemSelected += (sender, selectedItemChangedEventArgs) => 
            {
                Object selectedItem = selectedItemChangedEventArgs.SelectedItem.ToString();
                if (selectedItem is String)
                {
                    this.entry.Text = (String)selectedItem;
                    this.entry.Focus();
                }
            };
            this.listView.ItemTapped += (sender, tappedItemChangedEventArgs) =>
            {
                Object selectedItem = tappedItemChangedEventArgs.Item.ToString();
                if (selectedItem is String)
                {
                    this.entry.Text = (String)selectedItem;
                    this.entry.Focus();
                }
            };
        }

        //*** Initialization ***//

        public void Init(String historyIdentifier)
        {
            this.historyIdentifier = historyIdentifier;
            this.LoadHistory();
            this.initialized = true;
        }

        //*** Methods ***//

        public void AddToHistory(String value)
        {
            if (this.initialized)
            {
                if (value != null && !value.Equals(""))
                {
                    if (this.history.Contains(value))
                    {
                        this.history.Remove(value);
                    }
                    if (this.history.Count >= HISTORY_LIMIT)
                    {
                        this.history.RemoveAt(0);
                    }
                    this.history.Add(value);
                    this.SaveHistory();
                }
            }
        }

        private void SaveHistory()
        {
            // Saves the size of the hisotry
            Application.Current.Properties[this.historyIdentifier + "_size"] = this.history.Count;
            // Then saves all elements
            for ( int i = 0; i < this.history.Count; i++ )
            {
                Application.Current.Properties[this.historyIdentifier + "_" + i] = this.history.ElementAt<String>(i);
            }
        }

        private void LoadHistory()
        {
            // Gets the size of the history
            Object sizeObj;
            Application.Current.Properties.TryGetValue(this.historyIdentifier + "_size", out sizeObj);
            int size = 0;
            if (sizeObj != null && sizeObj is int)
            {
                size = (int) sizeObj;
            }
            // Clears and populates the history
            this.history.Clear();
            for (int i = 0; i < size; i++)
            {
                this.history.Add((String) Application.Current.Properties[this.historyIdentifier + "_" + i]);
            }

            //this.history = new List<String>();
            //this.history.Add("aaaa");
            //this.history.Add("bbb");
            //this.history.Add("abb");
        }

        private void UpdateHistoryInView(String value)
        {
            if (value == null) {
                value = "";
            }

            List<String> filteredHistory = new List<String>();

            //for (int i = this.history.Count - 1; i >= 0; i++)
            //{
            //    String element = this.history.ElementAt<String>(i);
            //    if (element.StartsWith(value))
            //    {
            //        filteredHistory.Add(element);
            //    }
            //}

            foreach (String element in this.history)
            {
                if (element.StartsWith(value))
                {
                    filteredHistory.Add(element);
                }
            }

            filteredHistory.Reverse();

            this.listView.ItemsSource = filteredHistory;
            int count = this.Count();
            this.listView.HeightRequest = count * this.listView.RowHeight;
        }

        private int Count()
        {
            int count = 0;
            foreach (var item in this.listView.ItemsSource)
            {
                count++;
            }
            return count;
        }
    }
}
