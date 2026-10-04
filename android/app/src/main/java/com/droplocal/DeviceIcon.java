package com.droplocal;
import android.content.Context;
import android.graphics.*;
import android.graphics.drawable.Drawable;

final class DeviceIcon extends Drawable {
    final boolean phone;final int size;final Paint paint=new Paint(Paint.ANTI_ALIAS_FLAG);
    DeviceIcon(Context context,boolean phone){this.phone=phone;size=DarkUi.dp(context,26);paint.setColor(DarkUi.ACCENT);paint.setStyle(Paint.Style.STROKE);paint.setStrokeWidth(1.7f);}
    @Override public int getIntrinsicWidth(){return size;}
    @Override public int getIntrinsicHeight(){return size;}
    @Override public void draw(Canvas canvas){Rect b=getBounds();canvas.save();canvas.translate(b.left,b.top);canvas.scale(b.width()/26f,b.height()/26f);if(phone){canvas.drawRoundRect(6,1,20,25,3,3,paint);canvas.drawLine(10,21,16,21,paint);}else{canvas.drawRoundRect(1,3,25,19,2,2,paint);canvas.drawLine(13,19,13,23,paint);canvas.drawLine(7,24,19,24,paint);}canvas.restore();}
    @Override public void setAlpha(int alpha){paint.setAlpha(alpha);}
    @Override public void setColorFilter(ColorFilter filter){paint.setColorFilter(filter);}
    @Override public int getOpacity(){return PixelFormat.TRANSLUCENT;}
}
