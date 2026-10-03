package com.droplocal;

import android.content.Context;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.StateListDrawable;
import android.text.TextUtils;
import android.view.Gravity;
import android.widget.*;
import java.util.Locale;

final class DarkUi {
    static final int BG = Color.rgb(12,24,37), PANEL = Color.rgb(24,41,57), FIELD = Color.rgb(31,50,68),
        BORDER = Color.rgb(53,77,98), TEXT = Color.rgb(239,245,251), MUTED = Color.rgb(163,183,203), ACCENT = Color.rgb(18,224,222), DANGER = Color.rgb(255,112,128);
    static int dp(Context c, int value) { return Math.round(c.getResources().getDisplayMetrics().density * value); }
    static GradientDrawable shape(Context c, int color, int stroke, int radius) {
        GradientDrawable d = new GradientDrawable(); d.setColor(color); d.setCornerRadius(dp(c,radius)); if (stroke != 0) d.setStroke(dp(c,1),stroke); return d;
    }
    static LinearLayout column(Context c) { LinearLayout v = new LinearLayout(c); v.setOrientation(LinearLayout.VERTICAL); return v; }
    static LinearLayout card(Context c) {
        LinearLayout card = column(c); card.setPadding(dp(c,18),dp(c,18),dp(c,18),dp(c,18)); card.setBackground(shape(c,PANEL,BORDER,16));
        LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(-1,-2); p.bottomMargin=dp(c,14); card.setLayoutParams(p); return card;
    }
    static TextView text(Context c, String text, int sp, int color, boolean bold) {
        TextView view = new TextView(c); view.setText(text); view.setTextSize(sp); view.setTextColor(color);
        view.setTypeface(Typeface.create("sans-serif",bold ? Typeface.BOLD : Typeface.NORMAL)); return view;
    }
    static void gap(Context c, LinearLayout parent, int height) { android.view.View space = new android.view.View(c); parent.addView(space,new LinearLayout.LayoutParams(1,dp(c,height))); }
    static Button button(Context c, String text, boolean primary, boolean danger) {
        Button button = new Button(c); button.setText(text); button.setTextSize(14); button.setAllCaps(false); button.setTypeface(Typeface.DEFAULT,Typeface.BOLD); button.setMinHeight(0); button.setMinimumHeight(0);
        int fg = primary ? BG : danger ? DANGER : ACCENT;
        button.setTextColor(new ColorStateList(new int[][] { new int[] {-android.R.attr.state_enabled}, new int[] {} },new int[] {MUTED,fg}));
        StateListDrawable states = new StateListDrawable(); states.addState(new int[] {-android.R.attr.state_enabled},shape(c,FIELD,BORDER,10));
        states.addState(new int[] {android.R.attr.state_pressed},shape(c,primary ? Color.rgb(93,243,237) : FIELD,fg,10));
        states.addState(new int[] {},shape(c,primary ? ACCENT : PANEL,primary ? ACCENT : fg,10)); button.setBackground(states);
        button.setPadding(dp(c,12),0,dp(c,12),0); button.setLayoutParams(new LinearLayout.LayoutParams(-1,dp(c,46))); return button;
    }
    static String size(long bytes) {
        if (bytes >= 1073741824L) return String.format(Locale.getDefault(),"%.2f GB",bytes/1073741824.0);
        if (bytes >= 1048576) return String.format(Locale.getDefault(),"%.2f MB",bytes/1048576.0);
        if (bytes >= 1024) return String.format(Locale.getDefault(),"%.1f KB",bytes/1024.0);
        return bytes + " B";
    }
    static LinearLayout fileRow(Context c,String name,long bytes) {
        LinearLayout row = new LinearLayout(c); row.setGravity(Gravity.CENTER_VERTICAL); row.setPadding(dp(c,12),dp(c,10),dp(c,12),dp(c,10)); row.setBackground(shape(c,FIELD,0,10));
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1,-2); params.bottomMargin=dp(c,7); row.setLayoutParams(params);
        String lower = name.toLowerCase(Locale.ROOT); boolean video = lower.endsWith(".mp4") || lower.endsWith(".mov"), music = lower.endsWith(".mp3") || lower.endsWith(".wav");
        TextView icon = text(c,video ? "▶" : music ? "♪" : "↓",18,BG,true); icon.setGravity(Gravity.CENTER); icon.setBackground(shape(c,video ? Color.rgb(141,103,239) : music ? Color.rgb(239,100,148) : ACCENT,0,7));
        row.addView(icon,new LinearLayout.LayoutParams(dp(c,32),dp(c,32)));
        LinearLayout names = column(c); LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(0,-2,1); nameParams.leftMargin=dp(c,12); names.setLayoutParams(nameParams);
        TextView fileName = text(c,name,14,TEXT,true); fileName.setMaxLines(2); fileName.setEllipsize(TextUtils.TruncateAt.END); names.addView(fileName); names.addView(text(c,size(bytes),12,MUTED,false)); row.addView(names); return row;
    }
}
