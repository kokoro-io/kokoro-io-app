﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

using KokoroIO.XamarinForms.Helpers;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public class RoomsViewModel : BaseViewModel
    {
        internal ApplicationViewModel Application { get; }

        public ObservableRangeCollection<Item> Items { get; set; }
        public Command LoadItemsCommand { get; set; }

        internal RoomsViewModel(ApplicationViewModel application)
        {
            Application = application;

            Title = "Rooms";
            Items = new ObservableRangeCollection<Item>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", async (obj, item) =>
            {
                var _item = item as Item;
                Items.Add(_item);
                await DataStore.AddItemAsync(_item);
            });
        }

        private async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await DataStore.GetItemsAsync(true);
                Items.ReplaceRange(items);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessagingCenter.Send(new MessagingCenterAlert
                {
                    Title = "Error",
                    Message = "Unable to load items.",
                    Cancel = "OK"
                }, "message");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}