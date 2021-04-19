using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace ExportReports
{
	public class Commands
	{
		[CommandMethod("ExportAlignmentName")]
		public static void ExportAlignmentName()
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;
			CivilDocument civDoc = CivilApplication.ActiveDocument;

			JObject alignNameCol = new JObject();

			using (Transaction trans = doc.TransactionManager.StartTransaction())
			{
				JArray alignName = new JArray();

				foreach (ObjectId alignId in civDoc.GetAlignmentIds())
				{
					Alignment align = trans.GetObject(alignId, OpenMode.ForRead) as Alignment;
					alignName.Add(align.Name);
				}

				alignNameCol.Add(new JProperty("name", alignName));
				trans.Commit();
			}

			using (StreamWriter file = File.CreateText("AlignmentName.json"))
			using (JsonTextWriter writer = new JsonTextWriter(file))
			{
				alignNameCol.WriteTo(writer);
			}
		}

		[CommandMethod("ExportReport")]
		public static void ExportReport()
		{
			Document acadDoc = Application.DocumentManager.MdiActiveDocument;
			CivilDocument civDoc = CivilApplication.ActiveDocument;

			List<MainPoint> mainPointCol = new List<MainPoint>();

			using (Transaction ts = acadDoc.Database.TransactionManager.StartTransaction())
			{
				// JSON に出力した線形名を取得
				dynamic inputParams = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(".\\AlignmentNameForReport.json"));
				string alignName = inputParams.name;

				// 線形を取得
				Alignment align = civDoc.GetAlignmentIds().Cast<ObjectId>()
					.Select(item => (Alignment)ts.GetObject(item, OpenMode.ForRead))
					.First(item => item.Name == alignName);

				// 線形の測点インデックス増分値を取得
				double stationIndexIncrement = align.StationIndexIncrement;

				// 線形の Entity オブジェクトを取得 -> 測点の順番で並べ替え
				IEnumerable<AlignmentCurve> alignCurveOrderedCol = align.Entities.Cast<AlignmentCurve>().OrderBy(item => item.StartStation);

				// 各測点の MainPoint オブジェクトを mainPointCol に追加
				int lineIndex = 0;
				int scsIndex = 0;

				foreach (AlignmentCurve alignCurve in alignCurveOrderedCol)
				{
					switch (alignCurve.EntityType)
					{
						case AlignmentEntityType.Line:
							AlignmentLine alignLine = alignCurve as AlignmentLine;
							mainPointCol.Add(new MainPoint(alignLine, lineIndex, stationIndexIncrement));
							lineIndex += 1;
							break;

						case AlignmentEntityType.SpiralCurveSpiral:
							AlignmentSCS alignSCS = alignCurve as AlignmentSCS;
							mainPointCol.Add(new MainPoint(alignSCS.SpiralIn, scsIndex, stationIndexIncrement) { });
							mainPointCol.Add(new MainPoint(alignSCS.Arc, scsIndex, stationIndexIncrement) { });
							mainPointCol.Add(new MainPoint(alignSCS.SpiralOut, scsIndex, stationIndexIncrement) { });
							scsIndex += 1;
							break;

						default:
							return;
					}

					try { _ = alignCurve.EntityAfter; }
					catch (EntityNotFoundException ex)
					{
						AlignmentLine alignLine = alignCurve as AlignmentLine;
						mainPointCol.Add(new MainPoint(alignLine, -1, stationIndexIncrement) { });
					}
				}

				ts.Commit();
			}

			List<string> header = new List<string>()
			{
				"主要点名称", "測点", "X 座標", "Y 座標", "要素長", "接線角", "始点半径", "終点半径", "パラメータ A"
			};

			string excelFilePath = ".\\Report.xlsx";
			using (XLWorkbook workbook = new XLWorkbook())
			{
				IXLWorksheet worksheet = workbook.AddWorksheet("Sheet1");
				worksheet.Cell("A1").InsertData(header, true);
				worksheet.Cell("A2").InsertData(mainPointCol);
				workbook.SaveAs(excelFilePath);
			}
		}
	}
	class MainPoint
	{
		public string name;
		public string station;
		public double pointX;
		public double pointY;
		public double? length;
		public double? direction;
		public double? radiusIn;
		public double? radiusOut;
		public double? aValue;

		public MainPoint(AlignmentLine alignLine, int index, double stationIndexIncrement)
		{
			name = index == 0 ? "BP"
				: index == -1 ? "EP"
				: $"KA{index}-2";
			station = (index == -1) ? FormatStation(alignLine.EndStation, stationIndexIncrement) : FormatStation(alignLine.StartStation, stationIndexIncrement);
			pointX = (index == -1) ? alignLine.EndPoint.X : alignLine.StartPoint.X;
			pointY = (index == -1) ? alignLine.EndPoint.Y : alignLine.StartPoint.Y;
			if (index != -1) { length = alignLine.Length; }
		}

		public MainPoint(AlignmentSubEntityArc alignArc, int index, double stationIndexIncrement)
		{
			double isCounterClockwise = alignArc.Clockwise ? -1 : 1;
			double radToDeg = 57.2958;

			name = $"KE{index + 1}-1";
			station = FormatStation(alignArc.StartStation, stationIndexIncrement);
			pointX = alignArc.StartPoint.X;
			pointY = alignArc.StartPoint.Y;
			length = alignArc.Length;
			direction = alignArc.StartDirection * radToDeg;
			radiusIn = alignArc.Radius * isCounterClockwise;
			radiusOut = alignArc.Radius * isCounterClockwise;
		}

		public MainPoint(AlignmentSubEntitySpiral alignSpiral, int index, double stationIndexIncrement)
		{
			double isCounterClockwise = (alignSpiral.Direction == SpiralDirectionType.DirectionRight) ? -1 : 1;
			double radToDeg = 57.2958;

			name = (alignSpiral.RadiusIn > alignSpiral.RadiusOut) ? $"KA{index + 1}-1" : $"KE{index + 1}-2";
			station = FormatStation(alignSpiral.StartStation, stationIndexIncrement);
			pointX = alignSpiral.StartPoint.X;
			pointY = alignSpiral.StartPoint.Y;
			length = alignSpiral.Length;
			direction = alignSpiral.StartDirection * radToDeg;
			radiusIn = alignSpiral.RadiusIn * isCounterClockwise;
			radiusOut = alignSpiral.RadiusOut * isCounterClockwise;
			aValue = alignSpiral.A * isCounterClockwise;
		}
		private protected string FormatStation(double station, double stationIndexIncrement)
		{
			double majorDist = Math.Floor(station / stationIndexIncrement);
			double minorDist = station % stationIndexIncrement;
			return $"{majorDist}+{minorDist}";
		}
	}
}
