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
  Controls.Add(new Label{Text=UiText.Get("SkinSelectionPanel_001"),AutoSize=true});Controls.Add(choices);Controls.Add(info);
  choices.Items.Add(new Choice("",new(){Name=UiText.Get("SkinSelectionPanel_003")}));
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
  info.Text=UiText.Get("SkinSelectionPanel_002")+folder+UiText.Get("SkinSelectionPanel_004")+(invalid>0?UiText.Format("SkinSelectionPanel_005",invalid):"");
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



