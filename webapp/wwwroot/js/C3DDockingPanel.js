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
	}

	addAlignmentNames(data) {
		for (var item of data.name) {
			const div = document.createElement('div')
			div.style.margin = '20px';
			div.style.cursor = 'pointer';
			div.innerText = item;
			div.onclick = (e) => {
				CustomRequestResponse.request("report", "/api/report", { "name": item })(this.options),
					CustomRequestResponse.response("report", "reportReady", "reportFailed",
						(url, res) => this.downloadReport(url)
					)
			};
			this.container.appendChild(div);
		}
	}

	downloadReport(url) {
		const a = document.createElement('a');
		a.href = url;
		a.download = "Report.xlsx";
		a.click();
		a.remove();
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
		this.onToolbarCreatedBinded = this.onToolbarCreated.bind(this);
		this.viewer.addEventListener(Autodesk.Viewing.TOOLBAR_CREATED_EVENT, this.onToolbarCreatedBinded);
		return true;
	};

	onToolbarCreated() {
		this.viewer.removeEventListener(Autodesk.Viewing.TOOLBAR_CREATED_EVENT, this.onToolbarCreatedBinded);
		this.onToolbarCreatedBinded = null;

		this.createUI();
		this.startConnection(
			() => CustomRequestResponse.request("alignment name", "/api/alignmentname"),
			() => CustomRequestResponse.response("alignment name", "alignNameReady", "alignNameFailed",
				(url, res) => this.panel.addAlignmentNames(JSON.parse(res))
			)
		)
	};

	unload() {
		this.viewer.toolbar.removeControl(this.subToolbar);
		return true;
	};

	createUI() {
		this.panel = new CustomDockingPanel(this.viewer, this.viewer.container, 'reportExtensionPanel', 'Choose alignments', this.options);

		this.button = new Autodesk.Viewing.UI.Button('toolbar-exportReportTool');
		this.button.setToolTip('ExportReport');
		this.button.onClick = (e) => {
			this.panel.setVisible(!this.panel.isVisible());
		};

		this.subToolbar = new Autodesk.Viewing.UI.ControlGroup('customTools');
		this.subToolbar.addControl(this.button);

		this.viewer.toolbar.addControl(this.subToolbar);
	};

	startConnection(req, res) {
		if (window._connection != undefined && window._connection.connectionState) {
			if (req) { req()(this.options); }
			if (res) { res(); }
			return;
		}

		window._connection = new signalR.HubConnectionBuilder().withUrl("/api/signalr/designautomation").build();
		window._connection.start()
			.then(() => window._connection.invoke('getConnectionId'))
			.then((id) => this.options.connectionId = id)
			.then(() => {
				if (req) { req()(this.options); }
				if (res) { res(); }
			});
	}
}

class CustomRequestResponse {
	static request(actionName, actionUrl, actionData) {
		return (options) => {
			$.notify(`Requesting ${actionName}.`, "info");
			if (!actionData) { actionData = {}; }

			jQuery.post({
				contentType: 'application/json',
				url: actionUrl,
				data: JSON.stringify({
					...actionData,
					'itemId': options.itemId,
					'versionId': options.versionId,
					'connectionId': options.connectionId
				}),
				success: (res) => {
					$.notify(`Succeeded to trigger workitem for ${actionName}.`, "info");
				},
				error: (err) => {
					$.notify(`Failed to trigger workitem for ${actionName}.`, "error");
				}
			});
		}
	}

	static response(actionName, successMethod, errorMethod, successAction) {
		window._connection.on(successMethod, (url) => {
			jQuery.get({
				url: url,
				success: (res) => {
					if (successAction) { successAction(url, res); }
					$.notify(`Succeeded to get ${actionName}.`, "success");
				},
				error: (err) => {
					$.notify(`Failed to read file for ${actionName}.`, "error");
				}
			});
		});

		window._connection.on(errorMethod, () => {
			$.notify(`Failed to run workitem for ${actionName}.`, "error");
		});
	}
}

Autodesk.Viewing.theExtensionManager.registerExtension('Autodesk.Sample.CustomDockingPanelExtension', CustomDockingPanelExtension);
