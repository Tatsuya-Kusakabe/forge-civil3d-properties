// *******************************************
// Custom Docking Panel
// *******************************************
class CustomDockingPanel extends Autodesk.Viewing.UI.DockingPanel {
	constructor(viewer, container, id, title, options) {
		super(container, id, title, options);
		this.viewer = viewer;

		// the style of the docking panel
		// use this built-in style to support Themes on Viewer 4+
		this.container.classList.add('docking-panel-container-solid-color-a');
		this.container.style.top = "10px";
		this.container.style.left = "10px";
		this.container.style.width = "auto";
		this.container.style.height = "auto";
		this.container.style.resize = "auto";

		// this is where we should place the content of our panel
		var div = document.createElement('div')
		div.style.margin = '20px';
		div.style.cursor = 'pointer';
		div.innerText = "My content here";
		div.onclick = (e) => { $.notify('Clicked.', 'success'); };
		this.container.appendChild(div);
	}
}

// *******************************************
// Custom Property Panel Extension
// *******************************************
class CustomDockingPanelExtension extends Autodesk.Viewing.Extension {
	constructor(viewer, options) {
		super(viewer, options);
		this.button = null;
		this.panel = null;
	}

	load() {
		if (this.viewer.toolbar) {
			// Toolbar is already available, create the UI
			this.createUI();
		} else {
			// Toolbar hasn't been created yet, wait until we get notification of its creation
			this.onToolbarCreatedBinded = this.onToolbarCreated.bind(this);
			this.viewer.addEventListener(Autodesk.Viewing.TOOLBAR_CREATED_EVENT, this.onToolbarCreatedBinded);
		}
		return true;
	};

	onToolbarCreated() {
		this.viewer.removeEventListener(Autodesk.Viewing.TOOLBAR_CREATED_EVENT, this.onToolbarCreatedBinded);
		this.onToolbarCreatedBinded = null;
		this.createUI();
	};

	createUI() {
		this.button = new Autodesk.Viewing.UI.Button('toolbar-exportReportTool');
		this.button.setToolTip('ExportReport');
		this.button.onClick = (e) => {
			if (this.panel === null) {
				this.panel = new CustomDockingPanel(this.viewer, this.viewer.container, 'reportExtensionPanel', 'Choose alignments');
			}
			this.panel.setVisible(!this.panel.isVisible());
		};

		this.subToolbar = new Autodesk.Viewing.UI.ControlGroup('customTools');
		this.subToolbar.addControl(this.button);

		this.viewer.toolbar.addControl(this.subToolbar);
	};

	unload() {
		this.viewer.toolbar.removeControl(this.subToolbar);
		return true;
	};
}

Autodesk.Viewing.theExtensionManager.registerExtension('Autodesk.Sample.CustomDockingPanelExtension', CustomDockingPanelExtension);
