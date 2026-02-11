using System;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using NXOpen;
using NXOpen.UF;

public class WcsAlignToFaceOrPlane
{
    private static Session theSession;
    private static UFSession theUf;
    private static Part wcsPart;

    private static Vector3d baseX;
    private static Vector3d baseZ;
    private static Point3d baseOrigin;

    private static bool reverseZ = false;
    private static int spinQuarterTurns = 0;
    private static double customAngleDeg = 0.0;

    public static int Main(string[] args)
    {
        theSession = Session.GetSession();
        theUf = UFSession.GetUFSession();
        wcsPart = (theSession.Parts.Display != null) ? theSession.Parts.Display : theSession.Parts.Work;

        try
        {
            TaggedObject selected;
            if (!SelectPlaneOrFace(out selected))
                return 0;

            if (!BuildFrameFromSelection(selected, out baseOrigin, out baseX, out baseZ))
            {
                ShowMessage("Seçilen geometri için eksen yönleri hesaplanamadı.");
                return 0;
            }

            ApplyWcs();
            ShowControlDialog();
        }
        catch (Exception ex)
        {
            ShowMessage("Hata: " + ex.Message);
        }

        return 0;
    }

    private static bool SelectPlaneOrFace(out TaggedObject selected)
    {
        selected = null;
        UI ui = UI.GetUI();

        Selection.MaskTriple[] masks = new Selection.MaskTriple[2];

        // Face seçimi (NX 2007 uyumlu maske)
        masks[0].Type = UFConstants.UF_solid_type;
        masks[0].Subtype = 0;
        masks[0].SolidBodySubtype = UFConstants.UF_UI_SEL_FEATURE_ANY_FACE;

        // Datum plane seçimi
        masks[1].Type = UFConstants.UF_datum_plane_type;
        masks[1].Subtype = 0;
        masks[1].SolidBodySubtype = 0;

        Point3d cursor;
        Selection.Response resp = ui.SelectionManager.SelectTaggedObject(
            "Bir yüzey veya datum düzlemi seçin",
            "WCS Hizalama",
            Selection.SelectionScope.AnyInAssembly,
            Selection.SelectionAction.ClearAndEnableSpecific,
            false,
            false,
            masks,
            out selected,
            out cursor);

        return resp == Selection.Response.Ok || resp == Selection.Response.ObjectSelected;
    }

    private static bool BuildFrameFromSelection(TaggedObject selected, out Point3d origin, out Vector3d xDir, out Vector3d zDir)
    {
        origin = new Point3d();
        xDir = new Vector3d();
        zDir = new Vector3d();

        Face face = selected as Face;
        if (face != null)
            return BuildFrameFromFace(face, out origin, out xDir, out zDir);

        DatumPlane datum = selected as DatumPlane;
        if (datum != null)
            return BuildFrameFromDatumPlane(datum, out origin, out xDir, out zDir);

        return false;
    }

    private static bool BuildFrameFromFace(Face face, out Point3d origin, out Vector3d xDir, out Vector3d zDir)
    {
        origin = new Point3d();
        xDir = new Vector3d();
        zDir = new Vector3d();

        Tag faceTag = face.Tag;

        double[] uvMinMax = new double[4];
        theUf.Modl.AskFaceUvMinmax(faceTag, uvMinMax);

        double uMin = uvMinMax[0];
        double uMax = uvMinMax[1];
        double vMin = uvMinMax[2];
        double vMax = uvMinMax[3];

        // Yüzey merkezi: UV orta noktası
        double uMid = 0.5 * (uMin + uMax);
        double vMid = 0.5 * (vMin + vMax);

        double[] pt = new double[3];
        double[] u1 = new double[3];
        double[] v1 = new double[3];
        double[] u2 = new double[3];
        double[] v2 = new double[3];
        double[] normal = new double[3];
        double[] radii = new double[2];

        theUf.Modl.AskFaceProps(faceTag, new double[] { uMid, vMid }, pt, u1, v1, u2, v2, normal, radii);

        origin = new Point3d(pt[0], pt[1], pt[2]);
        Vector3d uTan = Normalize(new Vector3d(u1[0], u1[1], u1[2]));
        Vector3d vTan = Normalize(new Vector3d(v1[0], v1[1], v1[2]));
        zDir = Normalize(new Vector3d(normal[0], normal[1], normal[2]));

        // X: en uzun parametre yönü
        Point3d pUMin = EvalFacePoint(faceTag, uMin, vMid);
        Point3d pUMax = EvalFacePoint(faceTag, uMax, vMid);
        Point3d pVMin = EvalFacePoint(faceTag, uMid, vMin);
        Point3d pVMax = EvalFacePoint(faceTag, uMid, vMax);

        double lenU = Distance(pUMin, pUMax);
        double lenV = Distance(pVMin, pVMax);

        xDir = (lenU >= lenV) ? uTan : vTan;
        xDir = Normalize(ProjectToPlane(xDir, zDir));

        return Magnitude(xDir) > 1e-6 && Magnitude(zDir) > 1e-6;
    }

    private static bool BuildFrameFromDatumPlane(DatumPlane datum, out Point3d origin, out Vector3d xDir, out Vector3d zDir)
    {
        origin = datum.Origin;
        zDir = Normalize(datum.Normal);

        Vector3d globalX = new Vector3d(1.0, 0.0, 0.0);
        xDir = Normalize(ProjectToPlane(globalX, zDir));

        if (Magnitude(xDir) < 1e-6)
        {
            Vector3d globalY = new Vector3d(0.0, 1.0, 0.0);
            xDir = Normalize(ProjectToPlane(globalY, zDir));
        }

        return Magnitude(xDir) > 1e-6 && Magnitude(zDir) > 1e-6;
    }

    private static Point3d EvalFacePoint(Tag faceTag, double u, double v)
    {
        double[] pt = new double[3];
        double[] u1 = new double[3];
        double[] v1 = new double[3];
        double[] u2 = new double[3];
        double[] v2 = new double[3];
        double[] normal = new double[3];
        double[] radii = new double[2];

        theUf.Modl.AskFaceProps(faceTag, new double[] { u, v }, pt, u1, v1, u2, v2, normal, radii);
        return new Point3d(pt[0], pt[1], pt[2]);
    }

    private static void ShowControlDialog()
    {
        Form form = new Form();
        form.Text = "WCS Eksen Kontrol";
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ClientSize = new Size(460, 230);
        form.BackColor = Color.FromArgb(245, 247, 250);
        form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        Panel header = new Panel();
        header.Left = 0;
        header.Top = 0;
        header.Width = form.ClientSize.Width;
        header.Height = 44;
        header.BackColor = Color.FromArgb(33, 150, 243);

        Label lblTitle = new Label();
        lblTitle.Left = 14;
        lblTitle.Top = 12;
        lblTitle.Width = 430;
        lblTitle.ForeColor = Color.White;
        lblTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.Text = "WCS Yön Kontrolü";

        header.Controls.Add(lblTitle);

        Button btnFlipZ = new Button();
        btnFlipZ.Text = "Z Ters Çevir";
        btnFlipZ.Left = 14;
        btnFlipZ.Top = 58;
        btnFlipZ.Width = 136;
        StylePrimaryButton(btnFlipZ);

        Button btnRotMinus = new Button();
        btnRotMinus.Text = "Z Etrafı -90°";
        btnRotMinus.Left = 162;
        btnRotMinus.Top = 58;
        btnRotMinus.Width = 136;
        StyleSecondaryButton(btnRotMinus);

        Button btnRotPlus = new Button();
        btnRotPlus.Text = "Z Etrafı +90°";
        btnRotPlus.Left = 310;
        btnRotPlus.Top = 58;
        btnRotPlus.Width = 136;
        StyleSecondaryButton(btnRotPlus);

        Label lblState = new Label();
        lblState.Left = 14;
        lblState.Top = 102;
        lblState.Width = 432;
        lblState.Height = 20;
        lblState.ForeColor = Color.FromArgb(55, 71, 79);
        lblState.Text = StateText();

        Label lblAngle = new Label();
        lblAngle.Left = 14;
        lblAngle.Top = 134;
        lblAngle.Width = 95;
        lblAngle.ForeColor = Color.FromArgb(69, 90, 100);
        lblAngle.Text = "Açı (derece):";

        TextBox txtAngle = new TextBox();
        txtAngle.Left = 112;
        txtAngle.Top = 130;
        txtAngle.Width = 95;
        txtAngle.BorderStyle = BorderStyle.FixedSingle;
        txtAngle.BackColor = Color.White;
        txtAngle.Text = "0";

        Button btnApplyAngle = new Button();
        btnApplyAngle.Text = "Açıyı Uygula";
        btnApplyAngle.Left = 217;
        btnApplyAngle.Top = 128;
        btnApplyAngle.Width = 136;
        StylePrimaryButton(btnApplyAngle);

        Button btnClose = new Button();
        btnClose.Text = "Kapat";
        btnClose.Left = 361;
        btnClose.Top = 128;
        btnClose.Width = 85;
        StyleSecondaryButton(btnClose);

        btnFlipZ.Click += delegate
        {
            reverseZ = !reverseZ;
            ApplyWcs();
            lblState.Text = StateText();
        };

        btnRotMinus.Click += delegate
        {
            spinQuarterTurns -= 1;
            ApplyWcs();
            lblState.Text = StateText();
        };

        btnRotPlus.Click += delegate
        {
            spinQuarterTurns += 1;
            ApplyWcs();
            lblState.Text = StateText();
        };

        btnApplyAngle.Click += delegate
        {
            double parsed;
            string value = txtAngle.Text.Trim().Replace(",", ".");
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                ShowMessage("Geçerli bir açı değeri girin. Örnek: 18.35");
                return;
            }

            customAngleDeg = parsed;
            ApplyWcs();
            lblState.Text = StateText();
        };

        btnClose.Click += delegate { form.Close(); };

        form.Controls.Add(header);
        form.Controls.Add(btnFlipZ);
        form.Controls.Add(btnRotMinus);
        form.Controls.Add(btnRotPlus);
        form.Controls.Add(lblState);
        form.Controls.Add(lblAngle);
        form.Controls.Add(txtAngle);
        form.Controls.Add(btnApplyAngle);
        form.Controls.Add(btnClose);

        form.ShowDialog();
        form.Dispose();
    }

    private static void StylePrimaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Color.FromArgb(33, 150, 243);
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        btn.Height = 32;
    }

    private static void StyleSecondaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = Color.FromArgb(176, 190, 197);
        btn.BackColor = Color.White;
        btn.ForeColor = Color.FromArgb(38, 50, 56);
        btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btn.Height = 32;
    }

    private static string StateText()
    {
        int deg = ((spinQuarterTurns % 4) + 4) % 4;
        return string.Format("Z: {0} | 90° adım: {1}° | Serbest açı: {2:0.###}°", reverseZ ? "Ters" : "Normal", deg * 90, customAngleDeg);
    }

    private static void ApplyWcs()
    {
        Vector3d z = reverseZ ? Scale(baseZ, -1.0) : baseZ;
        z = Normalize(z);

        double totalAngleDeg = spinQuarterTurns * 90.0 + customAngleDeg;
        double angle = totalAngleDeg * (Math.PI / 180.0);
        Vector3d x = RotateAroundAxis(baseX, z, angle);

        // x'i düzleme tekrar oturtup normalize et
        x = Normalize(ProjectToPlane(x, z));
        Vector3d y = Normalize(Cross(z, x));
        x = Normalize(Cross(y, z));

        Matrix3x3 m = new Matrix3x3();
        m.Xx = x.X; m.Xy = x.Y; m.Xz = x.Z;
        m.Yx = y.X; m.Yy = y.Y; m.Yz = y.Z;
        m.Zx = z.X; m.Zy = z.Y; m.Zz = z.Z;

        if (wcsPart != null)
            wcsPart.WCS.SetOriginAndMatrix(baseOrigin, m);
    }

    private static Vector3d RotateAroundAxis(Vector3d v, Vector3d axis, double angle)
    {
        // Rodrigues
        Vector3d k = Normalize(axis);
        double c = Math.Cos(angle);
        double s = Math.Sin(angle);

        Vector3d term1 = Scale(v, c);
        Vector3d term2 = Scale(Cross(k, v), s);
        Vector3d term3 = Scale(k, Dot(k, v) * (1.0 - c));

        return new Vector3d(term1.X + term2.X + term3.X,
                            term1.Y + term2.Y + term3.Y,
                            term1.Z + term2.Z + term3.Z);
    }

    private static Vector3d ProjectToPlane(Vector3d v, Vector3d normal)
    {
        double d = Dot(v, normal);
        return new Vector3d(v.X - d * normal.X, v.Y - d * normal.Y, v.Z - d * normal.Z);
    }

    private static Vector3d Normalize(Vector3d v)
    {
        double mag = Magnitude(v);
        if (mag < 1e-12) return new Vector3d(0, 0, 0);
        return new Vector3d(v.X / mag, v.Y / mag, v.Z / mag);
    }

    private static double Magnitude(Vector3d v)
    {
        return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
    }

    private static Vector3d Scale(Vector3d v, double s)
    {
        return new Vector3d(v.X * s, v.Y * s, v.Z * s);
    }

    private static Vector3d Cross(Vector3d a, Vector3d b)
    {
        return new Vector3d(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    private static double Dot(Vector3d a, Vector3d b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    private static double Distance(Point3d a, Point3d b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        double dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static void ShowMessage(string message)
    {
        NXOpen.UI.GetUI().NXMessageBox.Show("WCS Hizalama", NXMessageBox.DialogType.Information, message);
    }

    public static int GetUnloadOption(string dummy)
    {
        return (int)Session.LibraryUnloadOption.Immediately;
    }
}
