using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Smartproj.Utils
{
	public class XmlContainerAttribute : XmlElementAttribute
	{
		public readonly Type OwnerRefConastructorType;
		protected XmlContainerAttribute(string _name, Type _type, Type _owner = null)
			: base(_name, _type)
		{
            OwnerRefConastructorType = _owner;
		}
        protected XmlContainerAttribute(Type _type, Type _owner = null)
			: base(_type)
		{
            OwnerRefConastructorType = _owner;
        }
		public XmlContainerAttribute(Type _owner = null)
			: base()
		{
            OwnerRefConastructorType = _owner;
        }
	}
	public class XmlCollectionAttribute : XmlContainerAttribute
	{
        public readonly bool ItemIsContainer;
        public readonly Type ItemsType;
        public readonly bool ReadOnly;
        protected XmlCollectionAttribute(string _name, Type _type, bool _itemIsContainer, bool _readOnly, Type _itemsType, Type _owner = null)
			: base(_name, _type, _owner)
		{
			ItemIsContainer = _itemIsContainer;
			ItemsType = _itemsType;
			ReadOnly = _readOnly;
		}
        protected XmlCollectionAttribute(Type _type, bool _itemIsContainer, bool _readOnly, Type _itemsType, Type _owner = null)
			: base(_type, _owner)
		{
			ItemIsContainer = _itemIsContainer;
			ItemsType = _itemsType;
			ReadOnly = _readOnly;
		}
		public XmlCollectionAttribute(bool _itemIsContainer, bool _readOnly, Type _itemsType, Type _owner = null)
			: base(_owner)
		{
			ItemIsContainer = _itemIsContainer;
			ItemsType = _itemsType;
			ReadOnly = _readOnly;
        }
    }
	public static class Serializer
	{
		static Serializer()
		{
            //SerializerLog = new Logger();
			//SerializerLog.Open(@"C:\Users\g.karpuhov.FINEART-PRINT\source\repos\smartproj\bin\x64\Release\Serializer.txt");
        }
		public static Logger SerializerLog;
        public static readonly List<Type> TypesCache = new List<Type>()
		{
			typeof(object),
			typeof(bool),
			typeof(char),
			typeof(sbyte),
			typeof(short),
			typeof(int),
			typeof(long),
			typeof(ushort),
			typeof(uint),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(decimal),
			typeof(DateTime),
			typeof(string),
			typeof(Guid),
			typeof(Point),
			typeof(PointF),
			typeof(Rectangle),
			typeof(RectangleF),
			typeof(Size),
			typeof(SizeF),
            typeof(Color)
        };
		public static void LoadXml(this object _object, string _path)
		{
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(_path);
			LoadXml(_object, xmlDoc, null);
		}
		public static object LoadXml(string _path, params KeyValuePair<Type, object>[] _params)
		{
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(_path);
			return LoadXml(null, xmlDoc, _params);
		}
		public static object LoadXml(string _path)
		{
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(_path);
			return LoadXml(null, xmlDoc, null);
		}
		public static object LoadXml(object _object, XmlDocument _xmlDoc, params KeyValuePair<Type, object>[] _params)
		{
			// Загрузка типов 
			XmlNode typesNode = _xmlDoc.DocumentElement.SelectSingleNode(@"/root/Types");
			List<Type> types = new List<Type>(TypesCache);
			if (typesNode != null)
			{
				foreach (XmlNode typeNode in typesNode.ChildNodes)
				{
					Type t = Type.GetType(typeNode.InnerText);
					if (t != null)
					{
						types.Add(t);
					}
					else
						throw new InvalidOperationException("Ошибка инициализации типа " + typeNode.InnerText);
				}
			}

			XmlNode dataNode = _xmlDoc.DocumentElement.SelectSingleNode(@"/root/Data");
			if (dataNode == null || dataNode.ChildNodes.Count == 0)
			{
				return _object;
			}
			// Исходдний объект не определен
			if (_object == null)
			{
				Type[] pTypes = null;
				object[] pRefs = null;
				// Параметры в исходный конструктор
				if (_params != null && _params.Length > 0)
				{
					pTypes = new Type[_params.Length];
					pRefs = new object[_params.Length];
					for (int i = 0; i < _params.Length; i++)
					{
						pTypes[i] = _params[i].Key;
						pRefs[i] = _params[i].Value;
					}
				}
				else
					pTypes = Type.EmptyTypes;

				// dataNode корневой узел. поиск типа конструктора
				Type objType = types.FirstOrDefault(x => x.AssemblyQualifiedName.StringHashCode40() == Convert.ToInt32(dataNode.Attributes?["TypeId"]?.Value));
				if (objType != null)
				{
                    _object = objType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding | BindingFlags.Instance, null, pTypes, null).Invoke(pRefs);
                }
                else
                {
                    throw new InvalidOperationException("Не определен тип конструктора");
                }
            }

			Stack<KeyValuePair<XmlElement, object>> stack = new Stack<KeyValuePair<XmlElement, object>>();
			stack.Push(new KeyValuePair<XmlElement, object>((XmlElement)dataNode, _object));
			List<KeyValuePair<XmlElement, object>> graphs = new List<KeyValuePair<XmlElement, object>>();

            while (stack.Count > 0)
			{
				// Текущий объект и соответствующий узел сериализации
				KeyValuePair<XmlElement, object> current = stack.Pop();
				// Все свойства данного объекта
				PropertyInfo[] currentPropeties = current.Value.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
   
                foreach (XmlAttribute attribute in current.Key.Attributes)
				{
                    if (attribute.Name == "TypeId")
					{
						if (types.FirstOrDefault(x => x.AssemblyQualifiedName.StringHashCode40() == Convert.ToInt32(attribute.Value)) != current.Value.GetType())
						{
							throw new InvalidOperationException("Значение TypeId не соответствует текущему графу");
						}
					}
					else
					{
						// Поиск в классе текущего объекта свойства с именем, совпадающем  сименем аттрибута. У свойства должен быть доступ на запись и аттрибут сериализации
						PropertyInfo prop = currentPropeties.FirstOrDefault(x =>
						{
							if (x.CanWrite && String.Equals(x.Name, attribute.Name))
							{
								object[] propAttributes = x.GetCustomAttributes(true);
								foreach (var obj in propAttributes)
								{
									if (obj is XmlAttributeAttribute) return true;
								}
							}
							return false;
						});
						if (prop != null)
						{
							TypeConverter converter = TypeDescriptor.GetConverter(prop.PropertyType);
							if (converter == null)
							{
								throw new InvalidOperationException(String.Format("Не найден конвертор аттрибута типа '{0}' для его десериализации", prop.PropertyType.Name));
							}
							// Устанавливаем совйству значения из найденного xml аттрибута
                            prop.SetValue(current.Value, converter.ConvertFrom(attribute.Value), null);
						}
						// ПС. Пока не помню зачем реализована эта логика
					}
				}
            
                // Перебор узлов свойств класса текущего объекта
                foreach (XmlElement element in current.Key.ChildNodes)
				{
                    XmlAttribute idAttribute = element.Attributes?["TypeId"];
					// хэш типа текущего свойства
                    if (idAttribute != null)
					{
						int idAttributeValue = Convert.ToInt32(idAttribute.Value);
						Type dataType = null;
						PropertyInfo prop = currentPropeties.FirstOrDefault(x =>
						{
							Type propType = null;
							if (String.Equals(x.Name, element.Name) && (propType = types.FirstOrDefault(y => y.AssemblyQualifiedName.StringHashCode40() == idAttributeValue)) != null && x.PropertyType.IsAssignableFrom(propType))
							{
								dataType = propType;
								return true;
							}
							return false;
						});
						// Находим нужное свойство класса
						if (prop != null)
						{
							// Смотрим на аттрибуты найденного свойства
							foreach (var pAttribute in prop.GetCustomAttributes(true))
							{
								if (pAttribute is XmlElementAttribute)
								{
									// Есть атрибут сериализации. Проверяем определено ли значение
									if (String.Compare(element.InnerText, "null", true) != 0)
									{
										if (pAttribute is XmlContainerAttribute)
										{
											// Значение свойства - экземпляр класса
											object oldGraph = prop.GetValue(current.Value, null);
											// Смртрим что сейчас присвоено свойству
											object newGraph = null;

											if (oldGraph == null)
											{
												// В данный моменнт значение свойства неопределено
												if (prop.CanWrite)
												{
													// Если доступна запись, то создаем новый экземпляр класса и записываем
													if (((XmlContainerAttribute)pAttribute).OwnerRefConastructorType != null)
													{
														newGraph = dataType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding | BindingFlags.Instance, null, new Type[1] { ((XmlContainerAttribute)pAttribute).OwnerRefConastructorType }, null).Invoke(new object[1] { current.Value });
													}
													else
													{
														newGraph = dataType.GetConstructor(Type.EmptyTypes).Invoke(null);
													}
													prop.SetValue(current.Value, newGraph, null);
												}
												else
													throw new InvalidOperationException("Невозможно десериализовать объект так как свойство недоступно для записи - " + dataType.Name);
											}
											// Экземпляр класса - коллекция
											if (pAttribute is XmlCollectionAttribute)
											{
												bool isTreeNodesCollectionProperty = typeof(ITree).IsInstanceOfType(current.Value) && prop.Name == "TreeNodeItems";
												// Получаем методы добавления и очистки коллекции
												MethodInfo clear = null;
												MethodInfo add = null;
												IEnumerator enumerator = null;

												if (!((XmlCollectionAttribute)pAttribute).ReadOnly)
												{
													// Коллекция допускает изменения. Очищаем предыдущие данные и пробуем заполнить новыми
													if (isTreeNodesCollectionProperty)
													{
														// Если текущий объект - дерево, методы берем не из объекта коллекции, а из текущего узла ITree
														// Тут подразумевается что свойство коллекции - внутреняя коллекция узлов текущего узла. Напрямую изменять - нельзя!
														clear = current.Value.GetType().GetMethod("Clear");
														add = current.Value.GetType().GetMethod("Add", new Type[] { ((XmlCollectionAttribute)pAttribute).ItemsType });
													}
													else
													{
														clear = dataType.GetMethod("Clear");
														add = dataType.GetMethod("Add", new Type[] { ((XmlCollectionAttribute)pAttribute).ItemsType });
													}
													if ((add == null || clear == null))
													{
														throw new InvalidOperationException("Не найден метод 'Add/Clear' коллекции типа " + dataType.Name);
													}
													if (oldGraph != null)
													{
														// Если объект коллекции не новый - отчищаем все старые данные
														clear.Invoke(isTreeNodesCollectionProperty ? current.Value : oldGraph, null);
													}
												}
												else
												{
													// Коллекция только для чтения
													// Предполагается, что если коррекция только для чтения (недоступна для записи), значит там уже есть данные. Объект коллекции заново не создавался
													if (typeof(IEnumerable).IsInstanceOfType(oldGraph))
													{
														// Если можно, получаем энумератор
														enumerator = ((IEnumerable)oldGraph).GetEnumerator();
													}
													else
														break;
												}
												// Начинаем обрабатывать xml узлы записей коллекции
												foreach (XmlElement child in element.ChildNodes)
												{
													XmlAttribute itemIdAttribute = child.Attributes?["TypeId"];
													object collectionItem = null;
													// Если коллекция только для чтения, начинаем читать последовательно соответствующие записи
													if (enumerator != null)
													{
														if (enumerator.MoveNext())
														{
															collectionItem = enumerator.Current;
														}
														else
															throw new InvalidOperationException("Текущая коллекция не соответствует описанию xml");
													}

													if (itemIdAttribute != null)
													{
														int itemIdAttributeValue = Convert.ToInt32(itemIdAttribute.Value);
														Type itemDataType = types.FirstOrDefault(x => x.AssemblyQualifiedName.StringHashCode40() == itemIdAttributeValue);

														if (itemDataType != null)
														{
															// Аттрибут показывает что запись коллекции тоже вложенный класс для десериализации
															if (((XmlCollectionAttribute)pAttribute).ItemIsContainer)
															{
																// Создаем новый экземпляр объекта для добавления в коллекцию
																if (collectionItem == null)
																{
																	object newItem = itemDataType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
																	if (isTreeNodesCollectionProperty)
																	{
																		// Если дерево, используем метод родительского объекта
																		add.Invoke(current.Value, new object[] { newItem });
																	}
																	else
																	{
																		add.Invoke(oldGraph ?? newGraph, new object[] { newItem });
																	}
																	graphs.Add(new KeyValuePair<XmlElement, object>(child, newItem));
																}
																else
																	graphs.Add(new KeyValuePair<XmlElement, object>(child, collectionItem));
															}
															else
															{
																if (collectionItem == null)
																{
																	object value = null;
																	try
																	{
																		if (itemDataType.CanConvertFromString())
																		{
																			value = itemDataType.ContertFromString(child.InnerText);
																		}
																		else
																		{
																			TypeConverter itemConverter = TypeDescriptor.GetConverter(itemDataType);
																			if (itemConverter == null)
																			{
																				throw new InvalidOperationException(String.Format("Не найден конвертор типа '{0}' для его десериализации", dataType.Name));
																			}
																			value = itemConverter.ConvertFrom(child.InnerText);
																		}
																		add.Invoke(oldGraph ?? newGraph, new object[] { value });
																	}
																	catch (Exception ex)
																	{
																		SerializerLog?.WriteInfo("LoadXml", $"Exception = {ex.Message}");
																		SerializerLog?.WriteInfo("LoadXml", $"Exception = {ex.StackTrace}");
																		SerializerLog?.WriteInfo("LoadXml", $"itemDataType = {itemDataType.Name}");
																		SerializerLog?.WriteInfo("LoadXml", $"value = {value}");
																		SerializerLog?.WriteInfo("LoadXml", $"oldGraph ?? newGraph = {(oldGraph ?? newGraph).GetType().Name}");
																		SerializerLog?.WriteInfo("LoadXml", $"current = {current.Value?.GetType().Name}; isTreeNodesCollectionProperty = {isTreeNodesCollectionProperty}");
																		throw;
																	}
																}
															}

														}
														else
															throw new InvalidOperationException("Не найдена информация о типе. Code = " + itemIdAttributeValue.ToString());
													}
												}
											}
											else
											{
												graphs.Add(new KeyValuePair<XmlElement, object>(element, oldGraph ?? newGraph));
											}
										}
										else
										{
											try
											{
												if (prop.CanWrite)
												{
													// Если свойство имеет простой тип и доступно для записи, просто присваиваем значение
													if (dataType.CanConvertFromString())
													{
														prop.SetValue(current.Value, dataType.ContertFromString(element.InnerText), null);
													}
													else
													{
														TypeConverter converter = TypeDescriptor.GetConverter(dataType);
														if (converter == null)
														{
															throw new InvalidOperationException(String.Format("Не найден конвертор типа '{0}' для его десериализации", dataType.Name));
														}
														prop.SetValue(current.Value, converter.ConvertFrom(element.InnerText), null);
													}
												}
											}
											catch (Exception ex)
											{

												SerializerLog?.WriteInfo("LoadXml", $"Exception 1 = {ex.Message}");
												SerializerLog?.WriteInfo("LoadXml", $"Exception 1 = {ex.StackTrace}");
												SerializerLog?.WriteInfo("LoadXml", $"itemDataType = {dataType.Name}");
												SerializerLog?.WriteInfo("LoadXml", $"current.Value = {current.Value}");
												SerializerLog?.WriteInfo("LoadXml", $"current = {current.Value?.GetType().Name}");

												throw;
											}

										}
									}
									else
									{
										if (prop.CanWrite) prop.SetValue(current.Value, null, null);
									}

									break;
								}
							}
						}
						//else
							//throw new InvalidOperationException("Не найдено свойство соответствующеее узлу");
					}
					else
						throw new InvalidOperationException("Не определены аттрибуты узла");
				}

				foreach (var graph in graphs)
				{
					stack.Push(graph);
				}

				graphs.Clear();
			}

			return _object;
		}

		public static void SaveXml(this object _object, string _path)
		{
			XmlDocument xmlDoc = new XmlDocument();
			XmlElement root = xmlDoc.CreateElement("root");
			XmlElement data = xmlDoc.CreateElement("Data");

			xmlDoc.AppendChild(root);
			root.AppendChild(data);

			Stack<KeyValuePair<XmlElement, object>> stack = new Stack<KeyValuePair<XmlElement, object>>();
			stack.Push(new KeyValuePair<XmlElement, object>(data, _object));
			List<KeyValuePair<XmlElement, object>> graphs = new List<KeyValuePair<XmlElement, object>>();

			List<Type> types = new List<Type>();
			if (!TypesCache.Contains(_object.GetType()))
			{
				types.Add(_object.GetType());
			}

			data.SetAttribute("TypeId", _object.GetType().AssemblyQualifiedName.StringHashCode40().ToString());

			while (stack.Count > 0)
			{
				KeyValuePair<XmlElement, object> current = stack.Pop();

				foreach (PropertyInfo prop in current.Value.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					object[] attrs = prop.GetCustomAttributes(true);

					foreach (var att in attrs)
					{
						if (att is XmlElementAttribute)
						{
							XmlElement child = xmlDoc.CreateElement(prop.Name);

							object dataValue = prop.GetValue(current.Value, null);

							Type dataType = dataValue != null ? dataValue.GetType() : prop.PropertyType;

							if (!TypesCache.Contains(dataType) && !types.Contains(dataType))
							{
								types.Add(dataType);
							}

							child.SetAttribute("TypeId", dataType.AssemblyQualifiedName.StringHashCode40().ToString());

							if (dataValue != null)
							{
								if (att is XmlContainerAttribute)
								{
									if (att is XmlCollectionAttribute)
									{
										if (!typeof(IEnumerable).IsInstanceOfType(dataValue))
										{
											throw new InvalidOperationException("Коллекция для сериализации не реализует интерфейс IEnumerable");
										}

										IEnumerable collection = (IEnumerable)dataValue;

										foreach (var item in collection)
										{
											XmlElement childItem = xmlDoc.CreateElement("item");
											Type itemType = item != null ? item.GetType() : typeof(object);
											if (!TypesCache.Contains(itemType) && !types.Contains(itemType))
											{
												types.Add(itemType);
											}
											childItem.SetAttribute("TypeId", itemType.AssemblyQualifiedName.StringHashCode40().ToString());

											if (item != null)
											{
												if (((XmlCollectionAttribute)att).ItemIsContainer)
												{
													if (itemType.IsClass)
													{
														graphs.Add(new KeyValuePair<XmlElement, object>(childItem, item));
													}
													else
														throw new InvalidOperationException("Элемент коллекции не может быть графом так как не является классом");
												}
												else
												{
													TypeConverter converter;
													if (itemType.CanConvertToString())
													{
														childItem.InnerText = itemType.ContertToString(item);
													}
													else
													{
														if ((converter = TypeDescriptor.GetConverter(itemType)) != null)
														{
															childItem.InnerText = (string)converter.ConvertTo(item, typeof(string));
														}
														else
														{
															if (((XmlCollectionAttribute)att).ReadOnly)
															{
																childItem.InnerText = item.ToString();
															}
															else
																throw new InvalidOperationException(String.Format("Не найден кокнвертор типа '{0}' для его сериализации", itemType.Name));
														}
													}
												}
											}
											else
											{
												childItem.InnerText = "null";
											}

											child.AppendChild(childItem);
										}
									}
									else
										graphs.Add(new KeyValuePair<XmlElement, object>(child, dataValue));
								}
								else
								{
									TypeConverter converter;
                                    if (dataType.CanConvertToString())
                                    {
                                        child.InnerText = dataType.ContertToString(dataValue);
                                    }
									else
									{
										if ((converter = TypeDescriptor.GetConverter(dataType)) != null)
										{
											child.InnerText = (string)converter.ConvertTo(dataValue, typeof(string));
										}
										else
										{
											if (!prop.CanWrite)
											{
												child.InnerText = dataValue.ToString();
											}
											else
												throw new InvalidOperationException(String.Format("Не найден конвертор типа '{0}' для его сериализации", dataType.Name));
										}
									}
								}
							}
							else
							{
								child.InnerText = "null";
							}

							current.Key.AppendChild(child);

							break;
						}

						if (att is XmlAttributeAttribute)
						{
							TypeConverter converter;
							if ((converter = TypeDescriptor.GetConverter(prop.PropertyType)) != null)
							{
								object obj = prop.GetValue(current.Value, null);
								current.Key.SetAttribute(prop.Name, obj != null ? (string)converter.ConvertTo(obj, typeof(string)) : "null");
							}
							else
							{
								if (!prop.CanWrite)
								{
									object obj = prop.GetValue(current.Value, null);
									current.Key.SetAttribute(prop.Name, obj != null ? obj.ToString() : "null");
								}
								else
									throw new InvalidOperationException(String.Format("Не найден конвертор типа '{0}' для его сериализации Xml аттрибута", prop.Name));
							}

							break;
						}
					}
				}

				foreach (var graph in graphs)
				{
					stack.Push(graph);
				}

				graphs.Clear();
			}

			if (types.Count > 0)
			{
				XmlElement typesNode = xmlDoc.CreateElement("Types");
				root.AppendChild(typesNode);
				foreach (Type type in types)
				{
					string name = type.Name.Replace('`', '-').Replace('[', '_').Replace(']', '_');
					XmlElement node = xmlDoc.CreateElement(name);
					node.SetAttribute("Id", type.AssemblyQualifiedName.StringHashCode40().ToString());
					node.SetAttribute("Guid", type.GUID.ToString("P"));
					node.InnerText = type.AssemblyQualifiedName;
					typesNode.AppendChild(node);
				}
			}

			xmlDoc.Save(_path);
		}
	}
}
