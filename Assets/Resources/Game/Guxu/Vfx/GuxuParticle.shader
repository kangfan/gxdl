Shader "Guxu/Native Particle" {
 Properties { _MainTex ("Particle", 2D) = "white" {} }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  Cull Off Lighting Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   struct input { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
   struct output { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
   output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
   fixed4 frag(output i):SV_Target{return tex2D(_MainTex,i.uv)*i.color;}
   ENDCG
  }
 }
}
