//----------------------------------------------------------------
//<Author>xuyang</author>
//<CreateDate>19-10-9</createDate>
//<IssueNo></IssueNo>
//<Description>容大个案</description>
//----------------------------------------------------------------
//^_^ 20191009 add by xuyang for 容大个案
using System.Windows.Forms;
using Digiwin.Common.Torridity;
using Digiwin.Common.UI;
using Digiwin.ERP.Common.Utils;
namespace Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.UI.Implement {
    public partial class UpdateAvgConsumptionForm : DigiwinForm {
        public UpdateAvgConsumptionForm() {
            InitializeComponent();
        }

        public int CalculateMonth {
            get {
                return teAvgConsumption.Text.ToInt32();
            }
        }

        private void TeAvgConsumption_KeyPress(object sender, KeyPressEventArgs e) {
            if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8
                && e.KeyChar != 13 && e.KeyChar != 127
                && e.KeyChar != 1 && e.KeyChar != 3 && e.KeyChar != 4
                && e.KeyChar != 22 && e.KeyChar != 26 && e.KeyChar != 27) {
                e.Handled = true;
                return;
            }
            //可以通过粘贴方式把英文复制进去
            if (e.KeyChar == 22) {//考虑粘贴的情况
                string clipBoardText = Clipboard.GetText();
                //原先正则表达式无法匹配三位一撇和小数点都存在的情况，现在改成使用decimal.TryParse来做
                //todo:右键菜单现在是没有拦的，可以复制任何内容，究竟是否需要向这里一样智能
                //todo:“%”目前不支持
                int d;
                if (!int.TryParse(clipBoardText, out d)) {
                    e.Handled = true;
                    return;
                }
                if (clipBoardText.Contains("-")) {
                    e.Handled = true;
                    return;
                }
            }
            if (Maths.IsEmpty(teAvgConsumption.Text.Trim())) {
                teAvgConsumption.Text = "0";
                
            }
        }

    }
}
