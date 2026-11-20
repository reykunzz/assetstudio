# AssetStudio Android (tahap 1)

Port AssetStudio ke Android (.NET 8 Android). Memakai library inti `AssetStudio` (parser Unity),
UI Android sederhana: pilih file -> daftar object -> tap untuk export.

Tahap 1 (ada): load .assets/.bundle/.apk-extracted, list object, export TextAsset & dump struktur.
Tahap 2 (belum): decode Texture2D -> PNG (butuh Texture2DDecoderNative dibangun dengan NDK + ImageSharp),
Sprite, Mesh -> OBJ, AudioClip.
Tidak didukung: export FBX (butuh FBX SDK), konversi FSB (FMOD).

Lisensi: MIT (dari Perfare/AssetStudio), lihat LICENSE.
