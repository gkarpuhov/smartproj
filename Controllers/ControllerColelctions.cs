using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartproj
{
    /// <summary>
    /// Реализует коллекцию контроллеров <see cref="AbstractController"/>.
    /// Данный контейнер может нести разную смысловую нагрузку: 
    /// 1. При использовании на уровне <see cref="Smartproj.Project"/> представляет реализацию способа передачи исходных данных в систему для любых продуктов проекта в виде коллекции элементов <see cref="IInputProvider"/>. Если данный контейнер определен, то он имеет наивысший приоритет в очереди выполнения.
    /// 2. На уровне <see cref="AbstractInputProvider"/> представляет реализацию способы вывода результата обработки в виде коллекции <see cref="AbstractOutputProvider"/>. Если контейнер определен, то данный механиз будет отработан в самом конце всех действый.
    /// 3. На уровне объекта <see cref="Product"/> - контроллеры, определенные для конкретного типа продукта в виде коллекции объектов <see cref="AbstractController"/>. Аналогично могут реализовывать интерфейсы, как <see cref="IInputProvider"/>, так и <see cref="IOutputProvider"/>. Очередь выполнения определена между действиями п.1 и п.2 в порядке, определенном уменьшением значений свойства <see cref="AbstractController.Priority"/> 
    /// </summary>
    public class ControllerCollection : IEnumerable<AbstractController>
    {
        private Project mProject;
        private List<AbstractController> mItems;
        public int Count => mItems.Count;
        public Logger Log => Project?.Log;
        public Project Project
        {
            get
            {
                return mProject != null ? mProject : (mProject = Owner?.Owner?.Owner);
            }
            set 
            { 
                mProject = value; 
            }
        }
        public Product Owner { get; }
        public AbstractController this[Guid _uid] => mItems.Find(x => x.UID == _uid);
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_project"></param>
        /// <param name="_owner">Параметр определен только в случае реализации данного контейнера на уровне объекта <see cref="Product"/></param>
        public ControllerCollection(Project _project, Product _owner)
        {
            mItems = new List<AbstractController>();
            Owner = _owner;
            mProject = _project;
        }
        public AbstractController Add(AbstractController _controller)
        {
            if (_controller != null)
            {
                _controller.Owner = this;
                if (typeof(AbstractInputProvider).IsInstanceOfType(_controller) && ((AbstractInputProvider)_controller).DefaultOutput != null)
                {
                    ((AbstractInputProvider)_controller).DefaultOutput.Project = Project;
                }
                mItems.Add(_controller);
            }
            return _controller;
        }
        public void Clear()
        {
            foreach (var item in mItems)
            {
                item.Owner = null;
            }
            mItems.Clear();
        }
        public IEnumerator<AbstractController> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
    }

}
