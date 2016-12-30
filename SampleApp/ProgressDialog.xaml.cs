using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SampleApp
{
    public sealed partial class ProgressDialog : ContentDialog
    {
        public ProgressDialog()
        {
            this.InitializeComponent();
            this.Opened += delegate { Visible = true; };
            this.Closed += delegate { Visible = false; };
        }

        public void Update(int amountDone, int amountTotal)
        {
            if (Visible)
            {
                this.pbMain.Value = amountDone;
                this.pbMain.Maximum = amountTotal;
            }
        }

        public bool Visible = false;
    }
}
