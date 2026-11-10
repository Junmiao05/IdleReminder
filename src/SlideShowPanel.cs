// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SlideShowPanel:FlowLayoutPanel {
 readonly ThemedCheckBox enabled=new(){Text="幻灯片放映期间不提醒",AutoSize=true};
 readonly ThemedCheckBox ppt=new(){Text="Microsoft PowerPoint",AutoSize=true},wps=new(){Text="WPS 演示（兼容接口识别，部分版本需手动规则）",AutoSize=true};
 readonly ThemedComboBox windows=new(){Width=475,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly TextBox process=new(){Width=220,Text="wpp.exe"},className=new(){Width=475};
 readonly ListBox list=new(){Width=475,Height=100};
 readonly List<SlideWindowRule> rules;
 internal SlideShowPanel(Settings s){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  rules=s.SlideShow.WindowRules.Select(x=>new SlideWindowRule{ProcessName=x.ProcessName,WindowClass=x.WindowClass}).ToList();
  enabled.Checked=s.SlideShow.Enabled;ppt.Checked=s.SlideShow.PowerPoint;wps.Checked=s.SlideShow.Wps;
  Controls.Add(enabled);Controls.Add(ppt);Controls.Add(wps);
  Controls.Add(new Label{Text="编辑时正常提醒，放映（全屏或窗口化）时暂停闲置提醒。\n不以程序正在运行或任意窗口全屏作为放映依据。\n不读取幻灯片、文稿名称、窗口标题、备注或画面。\n退出放映后按实际键鼠闲置时间恢复，不连续补响。\n已有应用“运行中不提醒”规则仍会生效，请移除冲突规则。",AutoSize=true,MaximumSize=new Size(475,0)});
  Controls.Add(new Label{Text="WPS 版本无法识别时：默认正常检测，可配置具体放映窗口类。\n先进入放映，再读取窗口；不要选择编辑窗口通用类名。\n可取消对应软件自动检测，或删除手动规则，作为识别例外。\n手动规则不保证适用所有版本，请在编辑/放映两种状态验证。",AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText});
  Controls.Add(windows);
  Add("读取演示软件当前窗口",()=>{windows.Items.Clear();windows.Items.AddRange(SlideShowDetector.Windows().Cast<object>().ToArray());if(windows.Items.Count>0)windows.SelectedIndex=0;else MessageBox.Show("未发现 POWERPNT.EXE / wpp.exe 的可见窗口。");});
  windows.SelectedIndexChanged+=(_,_)=>{if(windows.SelectedItem is SlideWindow w){process.Text=w.ProcessName;className.Text=w.WindowClass;}};
  Controls.Add(new Label{Text="手动例外：软件进程名（仅 POWERPNT.EXE / wpp.exe）",AutoSize=true});Controls.Add(process);
  Controls.Add(new Label{Text="放映窗口类名（精确匹配）",AutoSize=true});Controls.Add(className);
  Add("添加放映窗口规则",()=>{
   var rule=new SlideWindowRule{ProcessName=process.Text.Trim(),WindowClass=className.Text.Trim()};SlideShowPolicy.Validate(rule);
   if(MessageBox.Show("请确认该类名只在放映窗口使用。若编辑窗口也使用同一类名，编辑时也会误暂停提醒。仍要添加？","手动规则确认",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
   if(!rules.Any(x=>x.ProcessName.Equals(rule.ProcessName,StringComparison.OrdinalIgnoreCase)&&x.WindowClass==rule.WindowClass))rules.Add(rule);Render();
  });
  Controls.Add(list);Add("删除所选规则",()=>{if(list.SelectedIndex>=0){rules.RemoveAt(list.SelectedIndex);Render();}});
  Render();
 }
 void Add(string text,Action action){var b=new Button{Text=text,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(Exception e){Store.Log("slideshow-ui-failed",e);MessageBox.Show(e is ArgumentException?e.Message:"读取演示软件窗口失败，请稍后重试。");}};Controls.Add(b);}
 void Render(){list.Items.Clear();foreach(var r in rules)list.Items.Add(r.ProcessName+" — "+r.WindowClass);}
 internal void Apply(Settings s){foreach(var r in rules)SlideShowPolicy.Validate(r);s.SlideShow=new(){Enabled=enabled.Checked,PowerPoint=ppt.Checked,Wps=wps.Checked,WindowRules=rules.Select(r=>new SlideWindowRule{ProcessName=r.ProcessName,WindowClass=r.WindowClass}).ToList()};}
}


