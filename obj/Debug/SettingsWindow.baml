   M S B A M L   `   `   `  ����C  ?BeMusic, Version=1.3.1.23, Culture=neutral, PublicKeyToken=null!clr-namespace:BeMusicBeMusic  5   	       BeMusic.SettingsWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   'localclr-namespace:BeMusic     5   	   D @ModernWpf, Version=0.9.4.0, Culture=neutral, PublicKeyToken=nullM IModernWpf.Controls, Version=0.9.4.0, Culture=neutral, PublicKeyToken=null,ui!http://schemas.modernwpf.com/2019   5   	   0  *ModernWpf.Controls.Primitives.WindowHelper    UseModernWindowStyle  .  � Title$ BeMusic - Settings��5
   	   $	��450��6$   $	��440��61   
���   �$	��440��6f   $	��250��6u   ��6P   Z� .��5      �� ���	#FF080808.��5   
   �� .+�5      � -   5         �Name$ SectionPreferences��5      $	��245��6/   $y�	Collapsed=�6<   .+�5      �� $��Preferences��6   $
��Left=�6.   $��	20,20,0,0q�6I   $	��Top=�6\   $��25(�6t   $
��Bold"�6�   %  ModernWpf.Controls.ToggleSwitch  5         Header$ 	Dark mode��6"      
OffContent$ On��65      	OnContent$	 Off��6E   $��Stretch=�6U   $��40,58,40,136q�6s   
������� J� IsHitTestVisible .  6�   5      � 5      -   5        $ SectionBackground��5      $	��245��6.   .+�5      �� $��
Background��6   $
��Left=�6-   $��	20,20,0,0q�6H   $	��Top=�6[   $��25(�6s   $
��Bold"�6�   �� 5      -   5         ��Name$ BackgroundImage��5      $��1q�62   $
��Left=�6F   $	��150��6a   $��	40,70,0,0q�6n   $	��Top=�6�   $	��150��6�    �� CornerRadius$ 10��6�   ��5      �� 5      -   5        BackgroundImageBrush��	 j� Stretch$	 UniformToFill=�5   C   
 �� ImageSource$+
 %Images/winter-mountain-snow-4k-01.jpg��6[   5      �� 5      -   5         ��Name$ ChangeImageButton��5      $��Change image��64   $
��Left=�6K   $��
215,70,0,0q�6f   $	��Top=�6z   $	��140��6�   6�   �� 5      -   5        $ DefaultBgImageButton��5      $��Default image��67   $
��Left=�6O   $��215,107,0,0q�6j   $	��Top=�6   $	��140��6�   6�   5      5      5      5      