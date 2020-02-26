using Rlcm.Util;

namespace Rlcm.Windows
{
    public partial class UpdateWindow
    {
        public UpdateWindow(Version version)
        {
            InitializeComponent();
            Message.Text = "Updating to version " + version.Name + "...";
        }
    }
}
