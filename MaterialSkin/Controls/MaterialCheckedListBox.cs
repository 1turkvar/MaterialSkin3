namespace MaterialSkin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Material tasarım prensiplerini takip eden özel CheckedListBox kontrolü.
    /// </summary>
    public class MaterialCheckedListBox : Panel, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        /// <summary>
        /// Çizgili bir arka plan görünümü sağlar.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Striped { get; set; }

        /// <summary>
        /// Çizgili görünümdeki koyu çizgilerin rengini belirler.
        /// </summary>
        [Category("Appearance")]
        public Color StripeDarkColor { get; set; }

        /// <summary>
        /// CheckedListBox öğelerini içerir.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ItemsList Items { get; private set; }

        /// <summary>
        /// Seçilen öğenin indeksi değiştiğinde tetiklenir.
        /// </summary>
        public event EventHandler<SelectedIndexChangedEventArgs> SelectedIndexChanged;

        /// <summary>
        /// Bir öğenin işaret durumu değiştiğinde tetiklenir.
        /// </summary>
        public event EventHandler<ItemCheckStateChangedEventArgs> ItemCheckStateChanged;

        /// <summary>
        /// MaterialCheckedListBox kontrolünün yapıcısı.
        /// </summary>
        public MaterialCheckedListBox() : base()
        {
            this.DoubleBuffered = true;
            this.Items = new ItemsList(this);
            this.AutoScroll = true;
            this.StripeDarkColor = Color.LightGray;
            this.Padding = new Padding(3);
        }

        /// <summary>
        /// Kontrol oluşturulduğunda çağrılır.
        /// </summary>
        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (DesignMode)
            {
                BackColorChanged += (sender, args) => BackColor = Parent?.BackColor ?? Color.White;
                BackColor = Parent?.BackColor ?? Color.White;
            }
            else
            {
                BackColorChanged += (sender, args) => BackColor = Parent != null ?
                    DrawHelper.BlendColor(Parent.BackColor, SkinManager.BackgroundAlternativeColor, SkinManager.BackgroundAlternativeColor.A) :
                    SkinManager.BackgroundColor;
                BackColor = Parent != null ?
                    DrawHelper.BlendColor(Parent.BackColor, SkinManager.BackgroundAlternativeColor, SkinManager.BackgroundAlternativeColor.A) :
                    SkinManager.BackgroundColor;
            }

            // Çizgili görünüm için Paint olayını ekleyelim
            if (Striped)
            {
                this.Paint += MaterialCheckedListBox_Paint;
            }
        }

        /// <summary>
        /// Çizgili görünüm için Paint olayı işleyicisi.
        /// </summary>
        private void MaterialCheckedListBox_Paint(object sender, PaintEventArgs e)
        {
            if (!Striped) return;

            // Çizgili görünüm çizme
            for (int i = 0; i < Items.Count; i += 2)
            {
                if (i < Items.Count && Items[i] != null)
                {
                    Rectangle rect = new Rectangle(0, Items[i].Top, Width, Items[i].Height);
                    using (SolidBrush brush = new SolidBrush(StripeDarkColor))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
            }
        }

        /// <summary>
        /// Belirtilen indeksteki öğenin işaret durumunu döndürür.
        /// </summary>
        /// <param name="index">Öğe indeksi</param>
        /// <returns>İşaret durumu</returns>
        public CheckState GetItemCheckState(int index)
        {
            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Geçersiz indeks.");

            return Items[index].CheckState;
        }

        /// <summary>
        /// Belirtilen indeksteki öğenin işaret durumunu ayarlar.
        /// </summary>
        /// <param name="index">Öğe indeksi</param>
        /// <param name="value">Yeni işaret durumu</param>
        public void SetItemCheckState(int index, CheckState value)
        {
            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Geçersiz indeks.");

            Items[index].CheckState = value;
        }

        /// <summary>
        /// Tüm öğeleri işaretler.
        /// </summary>
        public void CheckAll()
        {
            foreach (var item in Items)
            {
                item.Checked = true;
            }
        }

        /// <summary>
        /// Tüm öğelerin işaretini kaldırır.
        /// </summary>
        public void UncheckAll()
        {
            foreach (var item in Items)
            {
                item.Checked = false;
            }
        }

        /// <summary>
        /// İşaretli öğelerin indekslerini döndürür.
        /// </summary>
        /// <returns>İşaretli öğelerin indekslerini içeren bir liste</returns>
        public List<int> CheckedIndices()
        {
            List<int> indices = new List<int>();

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Checked)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        /// <summary>
        /// İşaretli öğeleri döndürür.
        /// </summary>
        /// <returns>İşaretli MaterialCheckbox nesnelerini içeren bir liste</returns>
        public List<MaterialCheckbox> CheckedItems()
        {
            List<MaterialCheckbox> checkedItems = new List<MaterialCheckbox>();

            foreach (var item in Items)
            {
                if (item.Checked)
                {
                    checkedItems.Add(item);
                }
            }

            return checkedItems;
        }

        /// <summary>
        /// Öğe seçildiğinde çağrılacak iç metod.
        /// </summary>
        internal void OnItemSelected(int index)
        {
            SelectedIndexChanged?.Invoke(this, new SelectedIndexChangedEventArgs(index));
        }

        /// <summary>
        /// Öğe işaret durumu değiştiğinde çağrılacak iç metod.
        /// </summary>
        internal void OnItemCheckStateChanged(int index, CheckState newState)
        {
            ItemCheckStateChanged?.Invoke(this, new ItemCheckStateChangedEventArgs(index, newState));
        }

        /// <summary>
        /// MaterialCheckedListBox için öğeler listesi.
        /// </summary>
        public class ItemsList : List<MaterialCheckbox>
        {
            private readonly Panel _parent;
            private int _itemHeight = 36; // Varsayılan öğe yüksekliği

            /// <summary>
            /// Öğe yüksekliği.
            /// </summary>
            public int ItemHeight
            {
                get => _itemHeight;
                set
                {
                    if (value < 24) value = 24; // Minimum yükseklik sınırı
                    _itemHeight = value;
                    UpdateItemsHeight();
                }
            }

            /// <summary>
            /// ItemsList yapıcısı.
            /// </summary>
            /// <param name="parent">Ana panel</param>
            public ItemsList(Panel parent)
            {
                _parent = parent;
            }

            /// <summary>
            /// Varsayılan değeri false olan bir MaterialCheckbox ekler.
            /// </summary>
            /// <param name="text">Öğe metni</param>
            public void Add(string text)
            {
                Add(text, false);
            }

            /// <summary>
            /// Belirtilen değere sahip bir MaterialCheckbox ekler.
            /// </summary>
            /// <param name="text">Öğe metni</param>
            /// <param name="defaultValue">Varsayılan işaret durumu</param>
            public void Add(string text, bool defaultValue)
            {
                MaterialCheckbox cb = new MaterialCheckbox();
                cb.Height = _itemHeight;
                cb.Checked = defaultValue;
                cb.Text = text;

                int index = Count;
                cb.CheckedChanged += (sender, e) =>
                {
                    if (_parent is MaterialCheckedListBox listBox)
                    {
                        listBox.OnItemCheckStateChanged(index, cb.CheckState);
                    }
                };

                cb.Click += (sender, e) =>
                {
                    if (_parent is MaterialCheckedListBox listBox)
                    {
                        listBox.OnItemSelected(index);
                    }
                };

                Add(cb);
            }

            /// <summary>
            /// Bir MaterialCheckbox öğesi ekler.
            /// </summary>
            /// <param name="value">Eklenecek MaterialCheckbox</param>
            public new void Add(MaterialCheckbox value)
            {
                base.Add(value);
                _parent.Controls.Add(value);
                value.Dock = DockStyle.Top;
                value.Height = _itemHeight;

                // Öğeyi Parent'ın en üstüne ekler, dolayısıyla görünümde en alta gider
                _parent.Controls.SetChildIndex(value, 0);
            }

            /// <summary>
            /// Bir MaterialCheckbox öğesini kaldırır.
            /// </summary>
            /// <param name="value">Kaldırılacak MaterialCheckbox</param>
            public new void Remove(MaterialCheckbox value)
            {
                base.Remove(value);
                _parent.Controls.Remove(value);
            }

            /// <summary>
            /// Belirtilen indeksteki öğeyi kaldırır.
            /// </summary>
            /// <param name="index">Kaldırılacak öğenin indeksi</param>
            public new void RemoveAt(int index)
            {
                if (index >= 0 && index < Count)
                {
                    MaterialCheckbox item = this[index];
                    Remove(item);
                }
            }

            /// <summary>
            /// Tüm öğelerin yüksekliklerini günceller.
            /// </summary>
            private void UpdateItemsHeight()
            {
                foreach (var item in this)
                {
                    item.Height = _itemHeight;
                }
            }

            /// <summary>
            /// Tüm öğeleri temizler.
            /// </summary>
            public new void Clear()
            {
                foreach (var item in ToArray())
                {
                    Remove(item);
                }
                base.Clear();
            }

            /// <summary>
            /// Belirtilen metne sahip öğeyi arar.
            /// </summary>
            /// <param name="text">Aranacak metin</param>
            /// <returns>Öğe indeksi, bulunamazsa -1</returns>
            public int IndexOf(string text)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].Text == text)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        /// Seçilen indeks değiştiğinde oluşturulan event argümanları.
        /// </summary>
        public class SelectedIndexChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Seçilen öğenin indeksi.
            /// </summary>
            public int Index { get; private set; }

            /// <summary>
            /// SelectedIndexChangedEventArgs yapıcısı.
            /// </summary>
            /// <param name="index">Seçilen öğenin indeksi</param>
            public SelectedIndexChangedEventArgs(int index)
            {
                Index = index;
            }
        }

        /// <summary>
        /// Öğe işaret durumu değiştiğinde oluşturulan event argümanları.
        /// </summary>
        public class ItemCheckStateChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Durumu değişen öğenin indeksi.
            /// </summary>
            public int Index { get; private set; }

            /// <summary>
            /// Öğenin yeni işaret durumu.
            /// </summary>
            public CheckState NewState { get; private set; }

            /// <summary>
            /// ItemCheckStateChangedEventArgs yapıcısı.
            /// </summary>
            /// <param name="index">Durumu değişen öğenin indeksi</param>
            /// <param name="newState">Yeni işaret durumu</param>
            public ItemCheckStateChangedEventArgs(int index, CheckState newState)
            {
                Index = index;
                NewState = newState;
            }
        }
    }
}