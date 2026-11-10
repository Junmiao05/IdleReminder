// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SkinSelectionPanel:FlowLayoutPanel {
 sealed record Choice(string File,SkinPresetDocument Skin){public override string ToString()=>Skin.Name;}
 readonly ThemedComboBox choices=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=360};
 readonly Form owner;
 readonly Label info=new(){AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText};
 internal SkinSelectionPanel(Settings settings,Form owner){
  this.owner=owner;AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  Controls.Add(new Label{Text="选择预设",AutoSize=true});Controls.Add(choices);Controls.Add(info);
  choices.Items.Add(new Choice("",new(){Name="默认（内置）"}));
  var folder=Path.Combine(Store.Root,"themes");int invalid=0;
  if(Directory.Exists(folder)){
   foreach(var file in Directory.EnumerateFiles(folder,"*.json").Order()){
    try{var name=Path.GetFileName(file);if(!SkinPresetDocument.IsSafeFilename(name)||name=="skin-designer-draft.json")continue;
     var doc=SkinPresetDocument.Load(file);if(doc.ImageSource!=null){using var png=CheckmarkAsset.LoadPng(doc.ImageSource);}choices.Items.Add(new Choice(name,doc));
    }catch(Exception ex){invalid++;Store.Log("skin-preset-load-failed",ex);}
   }
  }
  choices.SelectedIndex=0;
  for(int i=1;i<choices.Items.Count;i++)if(((Choice)choices.Items[i]!).File==settings.SkinPreset)choices.SelectedIndex=i;
  info.Text="预设来源："+folder+"\n新增或更新方案后重新打开设置窗口即可读取。\n颜色编辑请使用独立皮肤设计器；本页仅切换预设。\n选择后即时预览，点击保存设置后记住选择。"+(invalid>0?$"\n有 {invalid} 个无效方案未载入。":"");
  choices.SelectedIndexChanged+=(_,_)=>Preview();
 }
 internal void Preview(){if(choices.SelectedItem is Choice c)SkinPainter.Apply(owner,c.Skin);}
 internal void Apply(Settings settings){
  if(choices.SelectedItem is not Choice c)return;
  settings.SkinPreset=c.File;
  var palette=new CheckBoxPalette();foreach(var key in palette.Colors.Keys.ToArray())palette.Colors[key]=c.Skin.Colors["CheckBox."+key];
  settings.CheckBoxColors=palette;
 }
 internal void SelectFirstFileForVerification(){if(choices.Items.Count>1)choices.SelectedIndex=1;else Preview();}
}



