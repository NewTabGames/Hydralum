namespace HydraMenu.modules.self
{
	internal class NoZiplineCooldown : Module
	{
		public NoZiplineCooldown() : base("NoZiplineCooldown")
		{
			base.Enabled = true;
		}

		private void OnUseZipline(ZiplineConsole zipline)
		{
			if (zipline == null || zipline.destination == null) return;
			zipline.CoolDown = 0.0f;
			zipline.destination.CoolDown = 0.0f;
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnUseZipline += OnUseZipline;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnUseZipline -= OnUseZipline;
		}
	}
}