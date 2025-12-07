using System.Drawing;
using System.Windows.Forms;

namespace MultiCarrierManager
{
    public static class DarkTheme
    {
        public static readonly Color BackgroundDark = Color.FromArgb(30, 30, 30);
        public static readonly Color BackgroundMedium = Color.FromArgb(45, 45, 48);
        public static readonly Color BackgroundLight = Color.FromArgb(62, 62, 66);
        public static readonly Color TextPrimary = Color.FromArgb(241, 241, 241);
        public static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);
        public static readonly Color AccentOrange = Color.FromArgb(255, 140, 0);
        public static readonly Color AccentBlue = Color.FromArgb(0, 122, 204);
        public static readonly Color BorderColor = Color.FromArgb(67, 67, 70);
        public static readonly Color ButtonHover = Color.FromArgb(80, 80, 85);
        public static readonly Color SuccessGreen = Color.FromArgb(78, 154, 6);
        public static readonly Color ErrorRed = Color.FromArgb(204, 0, 0);

        public static void ApplyTheme(Form form)
        {
            form.BackColor = BackgroundDark;
            form.ForeColor = TextPrimary;
            ApplyThemeToControls(form.Controls);
        }

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                ApplyThemeToControl(control);
                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }

        private static void ApplyThemeToControl(Control control)
        {
            control.ForeColor = TextPrimary;

            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case TextBox textBox:
                    StyleTextBox(textBox);
                    break;
                case ComboBox comboBox:
                    StyleComboBox(comboBox);
                    break;
                case Label label:
                    StyleLabel(label);
                    break;
                case ListView listView:
                    StyleListView(listView);
                    break;
                case TabControl tabControl:
                    StyleTabControl(tabControl);
                    break;
                case TabPage tabPage:
                    StyleTabPage(tabPage);
                    break;
                case Panel panel:
                    panel.BackColor = BackgroundDark;
                    break;
                case CheckBox checkBox:
                    StyleCheckBox(checkBox);
                    break;
                case RadioButton radioButton:
                    StyleRadioButton(radioButton);
                    break;
                case GroupBox groupBox:
                    groupBox.BackColor = BackgroundDark;
                    groupBox.ForeColor = TextPrimary;
                    break;
                default:
                    control.BackColor = BackgroundDark;
                    break;
            }
        }

        public static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = AccentOrange;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ButtonHover;
            button.FlatAppearance.MouseDownBackColor = BackgroundLight;
            button.BackColor = BackgroundMedium;
            button.ForeColor = TextPrimary;
            button.Cursor = Cursors.Hand;
            button.Font = new Font(button.Font.FontFamily, button.Font.Size, FontStyle.Regular);
        }

        public static void StylePrimaryButton(Button button)
        {
            StyleButton(button);
            button.BackColor = AccentOrange;
            button.ForeColor = Color.Black;
            button.FlatAppearance.BorderColor = AccentOrange;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 160, 40);
            button.Font = new Font(button.Font.FontFamily, button.Font.Size, FontStyle.Bold);
        }

        public static void StyleDangerButton(Button button)
        {
            StyleButton(button);
            button.BackColor = ErrorRed;
            button.ForeColor = TextPrimary;
            button.FlatAppearance.BorderColor = ErrorRed;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 50, 50);
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = BackgroundMedium;
            textBox.ForeColor = TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.BackColor = BackgroundMedium;
            comboBox.ForeColor = TextPrimary;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        private static void StyleLabel(Label label)
        {
            label.ForeColor = TextPrimary;
            label.BackColor = Color.Transparent;
        }

        private static void StyleListView(ListView listView)
        {
            listView.BackColor = BackgroundMedium;
            listView.ForeColor = TextPrimary;
            listView.BorderStyle = BorderStyle.FixedSingle;
            listView.OwnerDraw = false;
        }

        private static void StyleTabControl(TabControl tabControl)
        {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
        }

        private static void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabControl = (TabControl)sender;
            TabPage page = tabControl.TabPages[e.Index];
            Rectangle tabBounds = tabControl.GetTabRect(e.Index);

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color tabBackColor = isSelected ? BackgroundLight : BackgroundMedium;
            Color tabTextColor = isSelected ? AccentOrange : TextPrimary;

            using (SolidBrush backBrush = new SolidBrush(tabBackColor))
            {
                e.Graphics.FillRectangle(backBrush, tabBounds);
            }

            StringFormat stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using (SolidBrush textBrush = new SolidBrush(tabTextColor))
            {
                e.Graphics.DrawString(page.Text, e.Font, textBrush, tabBounds, stringFormat);
            }
        }

        private static void StyleTabPage(TabPage tabPage)
        {
            tabPage.BackColor = BackgroundDark;
            tabPage.ForeColor = TextPrimary;
        }

        private static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.ForeColor = TextPrimary;
            checkBox.BackColor = Color.Transparent;
        }

        private static void StyleRadioButton(RadioButton radioButton)
        {
            radioButton.ForeColor = TextPrimary;
            radioButton.BackColor = Color.Transparent;
        }
    }
}
