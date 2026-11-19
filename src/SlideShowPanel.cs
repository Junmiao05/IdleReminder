// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SlideShowPanel:FlowLayoutPanel {
 readonly ThemedCheckBox enabled=new(){Text=UiText.Get("SlideShowPanel_001"),AutoSize=true};
 readonly ThemedCheckBox ppt=new(){Text="Microsoft PowerPoint",AutoSize=true},wps=new(){Text=UiText.Get("SlideShowPanel_002"),AutoSize=true};
 readonly ThemedComboBox windows=new(){Width=475,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly TextBox process=new(){Width=220,Text="wpp.exe"},className=new(){Width=475};
 readonly ListBox list=new(){Width=475,Height=100};
 readonly List<SlideWindowRule> rules;
 internal SlideShowPanel(Settings s){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  rules=s.SlideShow.WindowRules.Select(x=>new SlideWindowRule{ProcessName=x.ProcessName,WindowClass=x.WindowClass}).ToList();
  enabled.Checked=s.SlideShow.Enabled;ppt.Checked=s.SlideShow.PowerPoint;wps.Checked=s.SlideShow.Wps;
  Controls.Add(enabled);Controls.Add(ppt);Controls.Add(wps);
  Controls.Add(new Label{Text=UiText.Get("SlideShowPanel_003"),AutoSize=true,MaximumSize=new Size(475,0)});
  DetailsSection.Add(this,"SlideShowDetails",475,("SlideShowPanel_003Details",false),("SlideShowPanel_004",true));
  Controls.Add(windows);
  Add(UiText.Get("SlideShowPanel_009"),()=>{windows.Items.Clear();windows.Items.AddRange(SlideShowDetector.Windows().Cast<object>().ToArray());if(windows.Items.Count>0)windows.SelectedIndex=0;else MessageBox.Show(UiText.Get("SlideShowPanel_005"));});
  windows.SelectedIndexChanged+=(_,_)=>{if(windows.SelectedItem is SlideWindow w){process.Text=w.ProcessName;className.Text=w.WindowClass;}};
  Controls.Add(new Label{Text=UiText.Get("SlideShowPanel_006"),AutoSize=true});Controls.Add(process);
  Controls.Add(new Label{Text=UiText.Get("SlideShowPanel_007"),AutoSize=true});Controls.Add(className);
  Add(UiText.Get("SlideShowPanel_010"),()=>{
   var rule=new SlideWindowRule{ProcessName=process.Text.Trim(),WindowClass=className.Text.Trim()};SlideShowPolicy.Validate(rule);
   if(MessageBox.Show(UiText.Get("SlideShowPanel_008"),UiText.Get("SlideShowPanel_011"),MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
   if(!rules.Any(x=>x.ProcessName.Equals(rule.ProcessName,StringComparison.OrdinalIgnoreCase)&&x.WindowClass==rule.WindowClass))rules.Add(rule);Render();
  });
  Controls.Add(list);Add(UiText.Get("SlideShowPanel_012"),()=>{if(list.SelectedIndex>=0){rules.RemoveAt(list.SelectedIndex);Render();}});
  Render();
 }
 void Add(string text,Action action){var b=new ThemedButton{Text=text,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(Exception e){Store.Log("slideshow-ui-failed",e);MessageBox.Show(e is ArgumentException?e.Message:UiText.Get("SlideShowPanel_013"));}};Controls.Add(b);}
 void Render(){list.Items.Clear();foreach(var r in rules)list.Items.Add(r.ProcessName+" — "+r.WindowClass);}
 internal void Apply(Settings s){foreach(var r in rules)SlideShowPolicy.Validate(r);s.SlideShow=new(){Enabled=enabled.Checked,PowerPoint=ppt.Checked,Wps=wps.Checked,WindowRules=rules.Select(r=>new SlideWindowRule{ProcessName=r.ProcessName,WindowClass=r.WindowClass}).ToList()};}
}



