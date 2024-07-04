using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YandexDisk.Client.Http;
using YandexDisk.Client.Protocol;

namespace Smartproj
{
    public class VruAdapter : Adapter
    {
        private static bool GetYandexCloudItems(DiskHttpApi _api, string _current, string _filter, List<Resource> _items, bool _recursive, int _timeout)
        {
            try
            {
                Task<Resource> task = _api.MetaInfo.GetInfoAsync(new ResourceRequest { Path = _current, Limit = 1000 });

                if (task.Wait(_timeout * 1000))
                {
                    if (task.Result.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        foreach (var item in task.Result.Embedded.Items)
                        {
                            if (item.Type == ResourceType.Dir)
                            {
                                if (_recursive) GetYandexCloudItems(_api, item.Path, _filter, _items, _recursive, _timeout);
                            }
                            else
                            {
                                if (Regex.Match(item.Name, _filter, RegexOptions.IgnoreCase).Success) _items.Add(item);
                            }
                        }
                        return true;
                    }
                    else
                    {
                        //Log.WriteLine($"{DateTime.Now}: 'GetYandexCloudItems' GetInfoAsync Error -> {task.Result.HttpStatusCode}");
                    }
                }
                else
                {
                    //Log.WriteLine($"{DateTime.Now}: 'GetYandexCloudItems' GetInfoAsync Error -> Превышено время ожидания ответа от сервера");
                }
            }
            catch (Exception ex)
            {
                _items.Clear();
                //Log.WriteLine($"{DateTime.Now}: 'GetYandexCloudItems' Неожиданное исключение -> {ex.Message}");
            }
            return false;
        }
        protected override IEnumerable<object> GetRemote(RemoteSource _input, AbstractInputProvider _provider)
        {
            // Yandex Disk
            List<Resource> itemsFileInfo = new List<Resource>();
            if (_input.Mask == @"\.zip$")
            {
                // Пока доступны только архивы
                using (var API = new DiskHttpApi(_input.Auth))
                {
                    foreach (string source in _input.Link)
                    {
                        List<Resource> items = new List<Resource>();
                        GetYandexCloudItems(API, source, _input.Mask, items, false, _input.Timeout);
                        itemsFileInfo.AddRange(items);
                    }
                }
            }

            return itemsFileInfo;
        }
        protected override bool SetData(Project _project, AbstractInputProvider _provider, Job _job, object _sourceobject)
        {
            // Разбор содержимого метадаты из извлеченных файлов
            string metadata = "";
            string[] allproducts = Directory.GetFiles(Path.Combine(_project.Home, "Products"), "*.xml", SearchOption.AllDirectories);
            // Тут надо определить из исходных данных идентификатор продукта
            string productId = "5c9f8e22-e5b5-40b5-ad20-b3758d190ee8"; // Например
            string productFile = allproducts.SingleOrDefault(x => x.Contains(productId));
            // Тут надо определить из исходных данных формат продукта
            Size productSize = new Size(200, 280);  // Например
                                                    //
            _project.Log?.WriteInfo("VruAdapter.GetNext", $"Продукт  {_project.ProjectId} => {productId} ({productSize}) передан процессу {_job.UID} для инициализации");

            if (productFile != null && productFile != "")
            {
                try
                {
                    _job.OrderNumber = "1000";
                    _job.ItemId = "0000";
                    _job.ProductionQty = 1;
                    _job.Pages = 40;
                    //
                    _job.Create((Product)Serializer.LoadXml(productFile), productSize, metadata, MetadataType, FileDataFilter);
                    //_job.Product.Save();
                    _job.MinimalResolution = 200;
                    _project.Log?.WriteInfo("VruAdapter.GetNext", $"Продукт  {_project.ProjectId} => {_job.Product.ProductKeyCode} ({productSize.Width}X{productSize.Height}) успешно иницализирован процессом {_job.UID}");

                    return true;
                }
                catch (Exception ex)
                {
                    _project.Log?.WriteError("VruAdapter.GetNext", $"Ошибка при загрузке продукта '{productFile}': {ex.Message}");
                    _project.Log?.WriteError("VruAdapter.GetNext", $"Ошибка при загрузке продукта '{productFile}': {ex.StackTrace}");
                }
            }

            _project.Log?.WriteError("VruAdapter.GetNext", $"Процесс {_project.ProjectId} => {_job.UID}: ошибка загрузки продукта '{_project.ProjectId}' => {productId}");

            return false;
        }
        public VruAdapter() : base() 
        {
        }
    }
}
