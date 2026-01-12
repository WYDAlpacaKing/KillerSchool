Shader "Custom/SimpleSeaWater"
{
    Properties
    {
        [Header(Base Color)]
        _DeepColor ("Deep Water Color", Color) = (0, 0.2, 0.5, 1) // 深水颜色
        _ShallowColor ("Shallow Water Color", Color) = (0, 0.5, 0.8, 1) // 浅水/表面颜色
        
        [Header(Surface Details)]
        _MainTex ("Normal Map (Water Ripples)", 2D) = "bump" {} // 法线贴图（波纹）
        _Glossiness ("Smoothness", Range(0,1)) = 0.9 // 光滑度
        _Metallic ("Metallic", Range(0,1)) = 0.0 // 金属度
        
        [Header(Wave Movement)]
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 1.0 // 波浪速度
        _WaveHeight ("Wave Height", Range(0, 2)) = 0.5 // 波浪高度
        _WaveFrequency ("Wave Frequency", Range(0, 5)) = 1.0 // 波浪密度
        
        [Header(Texture Scrolling)]
        _ScrollXSpeed ("Ripple Scroll X", Range(-1, 1)) = 0.05 // 纹理流动速度 X
        _ScrollYSpeed ("Ripple Scroll Y", Range(-1, 1)) = 0.08 // 纹理流动速度 Y
    }
    
    SubShader
    {
        // 设置渲染队列为透明，忽略投影
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 200

        CGPROGRAM
        // 使用标准光照模型，启用顶点修改函数(vert)，启用透明混合(alpha)
        #pragma surface surf Standard vertex:vert alpha fullforwardshadows

        // 定义输入结构
        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir; // 视角方向，用于计算菲涅尔效应
        };

        // 声明变量（与Properties对应）
        fixed4 _DeepColor;
        fixed4 _ShallowColor;
        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        half _WaveSpeed;
        half _WaveHeight;
        half _WaveFrequency;
        half _ScrollXSpeed;
        half _ScrollYSpeed;

        // --- 1. 顶点着色器：控制海浪起伏 ---
        void vert (inout appdata_full v) 
        {
            // 获取时间变量
            float time = _Time.y * _WaveSpeed;
            
            // 使用 Sin 和 Cos 函数混合计算高度偏移
            // 加上 v.vertex.x 和 z 是为了让波浪随位置变化，而不是整个平面一起上下动
            float wave = sin(v.vertex.x * _WaveFrequency + time) * 0.5 + 
                         cos(v.vertex.z * _WaveFrequency + time * 0.8) * 0.5;
                         
            // 应用高度偏移到 Y 轴
            v.vertex.y += wave * _WaveHeight;
        }

        // --- 2. 表面着色器：控制颜色和纹理 ---
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 计算纹理流动的 UV 坐标
            float2 scrolledUV = IN.uv_MainTex;
            scrolledUV.x += _ScrollXSpeed * _Time.y;
            scrolledUV.y += _ScrollYSpeed * _Time.y;

            // 获取法线贴图（Normal Map）
            // 如果没有贴图，这一步不会报错，但水面会很平
            o.Normal = UnpackNormal(tex2D(_MainTex, scrolledUV));

            // --- 菲涅尔效应 (Fresnel) ---
            // 计算视线和法线的夹角。如果垂直看水面（中间），看到深色；如果平行看（远处），看到浅色反光。
            // dot(viewDir, Normal) 得到视角与法线的点积
            float fresnel = dot(normalize(IN.viewDir), o.Normal);
            fresnel = saturate(1.0 - fresnel); // 反转并限制在 0-1 之间

            // 混合深水和浅水颜色
            // pow(fresnel, 3) 是为了让边缘亮色更集中
            fixed4 c = lerp(_DeepColor, _ShallowColor, pow(fresnel, 3.0));

            // 设置输出
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a * 0.8; //稍微加一点透明度
        }
        ENDCG
    }
    FallBack "Diffuse"
}