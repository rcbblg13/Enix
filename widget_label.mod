ENTITY/pt,lb,v_cs,origin_pt,xdir_pt,ydir_pt,base_cs
NUMBER/m_sec,resp,cnt,n_orig,label_color,text_size,r_style,r_size,r_color,custom_size
STRING/t1(80),t2(80),t3(80)
IFTHEN/&ACTPRT == 1
  MESSG/'LUTFEN BIR WIDGET ACIN!'
  JUMP/TERM:
ENDIF
MASK/2
&CSIZE=4
&ENSITE=2
base_cs=&WCS
&LEADER=1
$$ VARSAYILAN AYARLAR
text_size=8
label_color=186
&CSIZE=text_size
$$ KULLANICI AYARLARI MENUSU (ILK GIRIS)
L_STYLE:
CHOOSE/'LABEL AYARLARI','DEVAM ET','YAZI BOYUTU','RENK',r_style
IFTHEN/r_style == 5
  JUMP/L_GM:
ELSEIF/r_style == 6
  JUMP/L_SIZE:
ELSEIF/r_style == 7
  JUMP/L_COLOR:
ENDIF
JUMP/TERM:
L_SIZE:
CHOOSE/'YAZI BOYUTU','1','2','3','4','5','6','8','9','10',$
'12','13','15','20','OZEL',r_size
IFTHEN/r_size < 5
  JUMP/L_STYLE:
ELSEIF/r_size == 5
  text_size=1
ELSEIF/r_size == 6
  text_size=2
ELSEIF/r_size == 7
  text_size=3
ELSEIF/r_size == 8
  text_size=4
ELSEIF/r_size == 9
  text_size=5
ELSEIF/r_size == 10
  text_size=6
ELSEIF/r_size == 11
  text_size=8
ELSEIF/r_size == 12
  text_size=9
ELSEIF/r_size == 13
  text_size=10
ELSEIF/r_size == 14
  text_size=12
ELSEIF/r_size == 15
  text_size=13
ELSEIF/r_size == 16
  text_size=15
ELSEIF/r_size == 17
  text_size=20
ELSEIF/r_size == 18
  JUMP/L_CUSTOM:
ENDIF
&CSIZE=text_size
JUMP/L_STYLE:
L_CUSTOM:
custom_size=INPUT/'OZEL YAZI BOYUTU GIRIN (ORN: 13.5)'
IFTHEN/custom_size > 0
  text_size=custom_size
  &CSIZE=text_size
ENDIF
JUMP/L_STYLE:
L_COLOR:
CHOOSE/'RENK SECIMI','KIRMIZI (186)','MAVI (211)','SARI (6)',$
'YESIL (36)','SIYAH (216)','BEYAZ (1)','MAGENTA (181)','MOR (200)',$
'TURUNCU (114)','MEDIUM PLUM (188)','MEDIUM FOREST (101)',$
'DEEP GOLD (156)','PALE BROWN (45)','CYAN (31)',r_color
IFTHEN/r_color < 5
  JUMP/L_STYLE:
ELSEIF/r_color == 5
  label_color=186
ELSEIF/r_color == 6
  label_color=211
ELSEIF/r_color == 7
  label_color=6
ELSEIF/r_color == 8
  label_color=36
ELSEIF/r_color == 9
  label_color=216
ELSEIF/r_color == 10
  label_color=1
ELSEIF/r_color == 11
  label_color=181
ELSEIF/r_color == 12
  label_color=200
ELSEIF/r_color == 13
  label_color=114
ELSEIF/r_color == 14
  label_color=188
ELSEIF/r_color == 15
  label_color=101
ELSEIF/r_color == 16
  label_color=156
ELSEIF/r_color == 17
  label_color=45
ELSEIF/r_color == 18
  label_color=31
ENDIF
JUMP/L_STYLE:
L_GM:
CHOOSE/'KOORDINAT SECIMI','X','Y','Z','XY','XZ','YZ','XYZ',$
'AYARLAR',RESP
IFTHEN/RESP == 12
  JUMP/L_STYLE:
ELSEIF/RESP >= 5
  m_sec = RESP - 4
  JUMP/L_GP:
ENDIF
JUMP/TERM:
L_GP:
GPOS/'NOKTA SECIN',x,y,z,rsp
IFTHEN/rsp == 3
  JUMP/L_GM:
ELSEIF/rsp == 2
  JUMP/L_GM:
ELSEIF/rsp < 1
  JUMP/TERM:
ENDIF
pt=POINT/x,y,z
n_orig=&DECPL
&DECPL=3
cnt=0
IFTHEN/m_sec == 1
  cnt=1
  t1='XC '+FSTR(x)
ELSEIF/m_sec == 2
  cnt=1
  t1='YC '+FSTR(y)
ELSEIF/m_sec == 3
  cnt=1
  t1='ZC '+FSTR(z)
ELSEIF/m_sec == 4
  cnt=2
  t1='XC '+FSTR(x)
  t2='YC '+FSTR(y)
ELSEIF/m_sec == 5
  cnt=2
  t1='XC '+FSTR(x)
  t2='ZC '+FSTR(z)
ELSEIF/m_sec == 6
  cnt=2
  t1='YC '+FSTR(y)
  t2='ZC '+FSTR(z)
ELSEIF/m_sec == 7
  cnt=3
  t1='XC '+FSTR(x)
  t2='YC '+FSTR(y)
  t3='ZC '+FSTR(z)
ENDIF
L_EP:
POS/'ETIKET KONUMU',xl,yl,zl,rsp
IFTHEN/rsp == 3
  &DECPL=n_orig
  DELETE/pt
  JUMP/L_GM:
ELSEIF/rsp == 2
  &DECPL=n_orig
  DELETE/pt
  JUMP/L_GM:
ELSEIF/rsp < 1
  &DECPL=n_orig
  DELETE/pt
  JUMP/TERM:
ENDIF
IFTHEN/x <= xl
  &LEADER=1
ELSE
  &LEADER=3
ENDIF
origin_pt=POINT/0,0,z
xdir_pt=POINT/1,0,z
ydir_pt=POINT/0,1,z
v_cs=CSYS/origin_pt,xdir_pt,ydir_pt
&WCS=v_cs
IFTHEN/cnt == 1
  lb=LABEL/xl,yl,x,y,t1
ELSEIF/cnt == 2
  lb=LABEL/xl,yl,x,y,t1,t2
ELSEIF/cnt == 3
  lb=LABEL/xl,yl,x,y,t1,t2,t3
ENDIF
&WCS=base_cs
DELETE/v_cs,origin_pt,xdir_pt,ydir_pt
DELETE/pt
&DECPL=n_orig
&COLOR(lb)=label_color
JUMP/L_GP:
TERM:
&WCS=base_cs
HALT
