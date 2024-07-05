using Emgu.CV.Dnn;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Smartproj
{
    public class MaterialsCollection : IEnumerable<Material>
    {
        private List<Material> mMaterials;
        public Product Owner { get; } 
        public MaterialsCollection this[string _detail] => new MaterialsCollection(mMaterials.Where(x => x.DetailId == _detail), Owner);
        public Material this[int _id] => mMaterials.SingleOrDefault(x => x.Id == _id);
        public Paper Paper => (Paper)mMaterials.FirstOrDefault(x => x.TypeId == 0);
        public Coating Coating => (Coating)mMaterials.FirstOrDefault(x => x.TypeId == 1);
        public void Add(Material _mat)
        {
            if (_mat == null || mMaterials.Contains(_mat)) return;

            // Для одного типа детали может быть только один материал того же типа
            for (int i = 0; i < mMaterials.Count; i++)
            {
                // Если уже был ранее добавлен - заменяем
                if (mMaterials[i].TypeId == _mat.TypeId && mMaterials[i].DetailId == _mat.DetailId) { mMaterials[i] = _mat; return; }
            }

            mMaterials.Add(_mat);
        }
        public IEnumerator<Material> GetEnumerator()
        {
            return mMaterials.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mMaterials.GetEnumerator();
        }
        protected MaterialsCollection(IEnumerable<Material> _items, Product _owner)
        {
            Owner = _owner;
            if (_items != null)
            {
                mMaterials = new List<Material>(_items);
            }
            else
            {
                mMaterials = new List<Material>();
            }
        }
        public MaterialsCollection(Product _owner) : this(null, _owner) 
        {
        }
    }
    public enum CoatedSideEnum
    {
        None,
        Front,
        Both
    }
    public class Coating : Material
    {
        public string Name { get; set; }
        public float Thickness { get; set; }
        public int Sides { get; set; }
        public Coating() : base("Плёнка для ламинации", 1, false) 
        {
        }
    }
    public class Paper : Material
    {
        public string Name { get; set; }
        public float Thickness { get; set; }
        public int Weight { get; set; }
        public bool IsRoll { get; set; }
        public bool IsDuplex { get; set; }
        public int CMCode { get; set; }
        public CoatedSideEnum Coated { get; set; }
        public Paper() : base("Бумага", 0, false) 
        {
            IsRoll = false;
            IsDuplex = true;
        }
    }
    public abstract class Material
    {
        public bool Workpiece { get; set; }
        public string Label { get; }
        // SWITCH
        public string SKU { get; set; }
        // MPP_SCUCODE
        public string SKU1 { get; set; }
        // XML_SCUCODE
        public string SKU2 { get; set; }
        public float Cost { get; set; }
        public string Description { get; set; }
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int TypeId { get; }
        public string DetailId { get; set; }
        public Size Size { get; set; }
        protected Material(string _label, int typeid, bool _workpiece) 
        {
            Label = _label;
            TypeId = typeid;
            Workpiece = _workpiece;
        }
    }
}
