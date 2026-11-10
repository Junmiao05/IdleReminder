// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Drawing.Imaging;
using System.Security.Cryptography;
namespace IdleReminder;
internal static class CheckmarkAsset {
 internal static Bitmap LoadPng(string path){
  var bytes=File.ReadAllBytes(path);
  if(bytes.Length<24||bytes.Length>2*1024*1024||!bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
   throw new ArgumentException("请选择有效 PNG，文件不能超过 2 MB。");
  int width=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4));
  int height=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4));
  if(width<1||height<1||width>512||height>512)throw new ArgumentException("PNG 尺寸须在 1～512 像素内，建议使用透明背景的方形图标。");
  using var stream=new MemoryStream(bytes);using var decoded=Image.FromStream(stream,true,true);
  if(decoded.RawFormat.Guid!=ImageFormat.Png.Guid)throw new ArgumentException("文件不是 PNG。");
  return new Bitmap(decoded);
 }
 internal static string Resolve(string folder,string relative)=>CheckmarkPath.Resolve(folder,relative);
 internal static string CopyInto(string source,string folder){
  using var verified=LoadPng(source);
  var bytes=File.ReadAllBytes(source);
  var relative="assets/checkmark-"+Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()+".png";
  var destination=Resolve(folder,relative);Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
  if(!File.Exists(destination))File.WriteAllBytes(destination,bytes);
  return relative;
 }
}
