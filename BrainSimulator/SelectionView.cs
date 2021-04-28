﻿using BrainSimulator.Modules;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainSimulator
{
    public partial class SelectionView
    {
        public static readonly DependencyProperty SelectionNumberProperty =
    DependencyProperty.Register("SelectionNumber", typeof(int), typeof(MenuItem));

        public static void CreateSelectionContextMenu(int i, ContextMenu cm = null) //for a selection
        {
            cmCancelled = false;
            if (cm == null)
                cm = new ContextMenu();
            StackPanel sp;
            cm.SetValue(SelectionNumberProperty, i);
            MenuItem mi = new MenuItem();
            mi = new MenuItem();
            mi.Header = "Cut";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Copy";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Delete";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);


            mi = new MenuItem();
            mi.Header = "Clear Selection";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Mutual Suppression";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Label { Content = "Random Synapses (Count): ", Padding = new Thickness(0) });
            sp.Children.Add(new TextBox { Text = "10", Width = 60, Name = "RandomSynapseCount", Padding = new Thickness(0) });
            mi = new MenuItem { Header = sp };
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Reset Hebbian Weights";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Label { Content = "Convert to Module: ", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0) });
            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            ComboBox cb = new ComboBox();
            //get list of available NEW modules...these are assignable to a "ModuleBase" 
            var listOfBs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                            from assemblyType in domainAssembly.GetTypes()
                            where typeof(ModuleBase).IsAssignableFrom(assemblyType)
                            orderby assemblyType.Name
                            select assemblyType
                            ).ToArray();
            foreach (var v in listOfBs)
            {
                if (v.Name != "ModuleBase")
                {
                    Type t = Type.GetType(v.FullName);
                    if (v.Name != "ModuleBase")
                    {
                        string theName = v.Name.Replace("Module", "");
                        cb.Items.Add(theName);
                    }
                }
            }
            cb.Width = 80;
            cb.Name = "AreaType";
            cb.SelectionChanged += Cb_SelectionChanged;
            sp.Children.Add(new MenuItem { Header = cb, StaysOpenOnClick = true });

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            Button b0 = new Button { Content = "OK", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);
            b0 = new Button { Content = "Cancel", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);

            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            cm.Closed += Cm_Closed;
        }

        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
                if (cb.Parent is StackPanel sp)
                    if (sp.Parent is MenuItem mi)
                        if (mi.Parent is ContextMenu cm)
                            cm.IsOpen = false;
        }

        static bool cmCancelled = false;
        private static void B0_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                if (b.Parent is StackPanel sp)
                {
                    if (sp.Parent is MenuItem mi)
                    {
                        if (mi.Parent is ContextMenu cm)
                        {
                            if ((string)b.Content == "Cancel")
                                cmCancelled = true;
                            Cm_Closed(cm, e);
                        }
                    }
                }
            }
        }


        static bool deleted = false;
        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) > 0)
            {
                MainWindow.Update();
                return;
            }
            if (deleted)
            {
                deleted = false;
            }
            else if (sender is ContextMenu cm)
            {
                if (!cm.IsOpen) return;
                cm.IsOpen = false;
                if (cmCancelled) return;

                int i = (int)cm.GetValue(SelectionNumberProperty);
                string label = "";
                string commandLine = "";
                Color color = Colors.Wheat;
                int width = 1, height = 1;

                Control cc = Utils.FindByName(cm, "AreaType");
                if (cc is ComboBox cb && cb.SelectedValue != null)
                {
                    commandLine = "Module" + (string)cb.SelectedValue;
                    if (commandLine == "") return;//something went wrong
                    label = (string)cb.SelectedValue;
                }

                if (label == "" && commandLine == "") return;
                //convert a selection rectangle to a module
                MainWindow.theNeuronArray.SetUndoPoint();
                MainWindow.arrayView.DeleteSelection(true);
                MainWindow.theNeuronArray.AddModuleUndo(MainWindow.theNeuronArray.modules.Count, null);
                width = MainWindow.arrayView.theSelection.selectedRectangles[i].Width;
                height = MainWindow.arrayView.theSelection.selectedRectangles[i].Height;
                SelectionRectangle nsr = MainWindow.arrayView.theSelection.selectedRectangles[i];
                MainWindow.arrayView.theSelection.selectedRectangles.RemoveAt(i);
                CreateModule(label, commandLine, color, nsr.FirstSelectedNeuron, width, height);
            }
            MainWindow.Update();
        }

        public static void CreateModule(string label, string commandLine, Color color, int firstNeuron, int width, int height)
        {
            ModuleView mv = new ModuleView(firstNeuron, width, height, label, commandLine, Utils.ColorToInt(color));
            if (mv.Width < mv.TheModule.MinWidth) mv.Width = mv.TheModule.MinWidth;
            if (mv.Height < mv.TheModule.MinHeight) mv.Height = mv.TheModule.MinHeight;
            MainWindow.theNeuronArray.modules.Add(mv);
            string[] Params = commandLine.Split(' ');
            if (mv.TheModule != null)
            {
                //MainWindow.theNeuronArray.areas[i].TheModule.Initialize(); //doesn't work with camera module
            }
            else
            {
                Type t1x = Type.GetType("BrainSimulator.Modules." + Params[0]);
                if (t1x != null && (mv.TheModule == null || mv.TheModule.GetType() != t1x))
                {
                    mv.TheModule = (ModuleBase)Activator.CreateInstance(t1x);
                    //  MainWindow.theNeuronArray.areas[i].TheModule.Initialize();
                }
            }
        }

        private static void Mi_Click(object sender, RoutedEventArgs e)
        {
            //Handle delete  & initialize commands
            if (sender is MenuItem mi)
            {
                if (mi.Header is StackPanel sp && sp.Children[0] is Label l && l.Content.ToString().StartsWith("Random"))
                {
                    if (sp.Children[1] is TextBox tb0)
                    {
                        if (int.TryParse(tb0.Text, out int count))
                        {
                            MainWindow.arrayView.CreateRandomSynapses(count);
                            MainWindow.theNeuronArray.ShowSynapses = true;
                            MainWindow.thisWindow.SetShowSynapsesCheckBox(true);
                            MainWindow.Update();
                        }
                    }
                    return;
                }
                if ((string)mi.Header == "Cut")
                {
                    MainWindow.arrayView.CutNeurons();
                    MainWindow.Update();
                }
                if ((string)mi.Header == "Copy")
                {
                    MainWindow.arrayView.CopyNeurons();
                }
                if ((string)mi.Header == "Clear Selection")
                {
                    MainWindow.arrayView.ClearSelection();
                    MainWindow.Update();
                }
                if ((string)mi.Header == "Mutual Suppression")
                {
                    MainWindow.arrayView.MutualSuppression();
                    MainWindow.theNeuronArray.ShowSynapses = true;
                    MainWindow.thisWindow.SetShowSynapsesCheckBox(true);
                    MainWindow.Update();
                }
                if ((string)mi.Header == "Delete")
                {
                    int i = (int)mi.Parent.GetValue(SelectionNumberProperty);
                        MainWindow.arrayView.DeleteSelection();
                }
                if ((string)mi.Header == "Reset Hebbian Weights")
                {
                    MainWindow.theNeuronArray.SetUndoPoint();
                    foreach (SelectionRectangle sr in MainWindow.arrayView.theSelection.selectedRectangles)
                    {
                        foreach (int Id in sr.NeuronInRectangle())
                        {
                            Neuron n = MainWindow.theNeuronArray.GetNeuron(Id);
                            foreach (Synapse s in n.Synapses)
                            {
                                if (s.model != Synapse.modelType.Fixed)
                                {
                                    //TODO: Add some UI for this:
                                    //s.model = Synapse.modelType.Hebbian2;
                                    n.AddSynapseWithUndo(s.targetNeuron, 0, s.model);
                                    s.Weight = 0;
                                }
                            }
                        }
                    }
                    MainWindow.Update();
                }
            }
        }
    }
}
