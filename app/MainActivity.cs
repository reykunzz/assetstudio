using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AS = AssetStudio;

namespace AssetStudioAndroid
{
    [Activity(Label = "AssetStudio", MainLauncher = true, Exported = true)]
    public class MainActivity : Activity
    {
        const int PickFileRequest = 1001;

        readonly List<AS.Object> _objects = new List<AS.Object>();
        readonly List<string> _rows = new List<string>();
        AS.AssetsManager _manager;
        ArrayAdapter<string> _adapter;
        TextView _status;
        ListView _list;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
            root.SetPadding(24, 48, 24, 24);

            var btnPick = new Button(this) { Text = "Pilih file (assets / bundle / apk)" };
            btnPick.Click += (s, e) => PickFile();
            root.AddView(btnPick);

            _status = new TextView(this) { Text = "Belum ada file." };
            root.AddView(_status);

            _list = new ListView(this);
            _adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, _rows);
            _list.Adapter = _adapter;
            _list.ItemClick += (s, e) => ExportOne(e.Position);
            root.AddView(_list, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 0, 1f));

            SetContentView(root);
        }

        void PickFile()
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            StartActivityForResult(intent, PickFileRequest);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode != PickFileRequest || resultCode != Result.Ok || data?.Data == null)
                return;

            var uri = data.Data;
            Task.Run(() => LoadFromUri(uri));
        }

        void LoadFromUri(Android.Net.Uri uri)
        {
            try
            {
                RunOnUiThread(() => _status.Text = "Menyalin & memuat file...");

                var cacheDir = Path.Combine(CacheDir.AbsolutePath, "input");
                Directory.CreateDirectory(cacheDir);
                var local = Path.Combine(cacheDir, "input.bin");

                using (var input = ContentResolver.OpenInputStream(uri))
                using (var output = File.Create(local))
                    input.CopyTo(output);

                _manager?.Clear();
                _manager = new AS.AssetsManager();
                _manager.LoadFiles(local);

                var objs = new List<AS.Object>();
                var rows = new List<string>();
                foreach (var file in _manager.assetsFileList)
                {
                    foreach (var obj in file.Objects)
                    {
                        objs.Add(obj);
                        var name = (obj as AS.NamedObject)?.m_Name ?? "";
                        rows.Add($"[{obj.type}] {name}  ({obj.byteSize} B)");
                    }
                }

                RunOnUiThread(() =>
                {
                    _objects.Clear(); _objects.AddRange(objs);
                    _rows.Clear(); _rows.AddRange(rows);
                    _adapter.NotifyDataSetChanged();
                    _status.Text = $"{objs.Count} object dimuat. Tap item untuk export.";
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => _status.Text = "Gagal: " + ex.Message);
            }
        }

        void ExportOne(int index)
        {
            if (index < 0 || index >= _objects.Count) return;
            var obj = _objects[index];
            try
            {
                var dir = Path.Combine(GetExternalFilesDir(null).AbsolutePath, "export", obj.type.ToString());
                Directory.CreateDirectory(dir);

                var name = (obj as AS.NamedObject)?.m_Name;
                if (string.IsNullOrWhiteSpace(name)) name = obj.m_PathID.ToString();
                foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

                string outPath;
                if (obj is AS.TextAsset ta)
                {
                    outPath = Path.Combine(dir, name + ".txt");
                    File.WriteAllBytes(outPath, ta.m_Script);
                }
                else
                {
                    // Tahap 1: dump struktur (teks) lewat TypeTree bila ada, selain itu raw bytes.
                    var dump = SafeDump(obj);
                    if (dump != null)
                    {
                        outPath = Path.Combine(dir, name + ".txt");
                        File.WriteAllText(outPath, dump, Encoding.UTF8);
                    }
                    else
                    {
                        outPath = Path.Combine(dir, name + ".bin");
                        File.WriteAllBytes(outPath, obj.GetRawData());
                    }
                }
                Toast.MakeText(this, "Tersimpan: " + outPath, ToastLength.Long).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Export gagal: " + ex.Message, ToastLength.Long).Show();
            }
        }

        static string SafeDump(AS.Object obj)
        {
            try { return obj.Dump(); } catch { return null; }
        }
    }
}
