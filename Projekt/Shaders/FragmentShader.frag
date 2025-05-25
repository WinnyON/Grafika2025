#version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;
        uniform vec3 uAmbientStrength;
        uniform vec3 uDiffuseStrength;
        uniform vec3 uSpecularStrength;

        uniform sampler2D uTexture;

        out vec4 FragColor;

		in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;
        in vec2 outTexture;

        void main()
        {
            //float ambientStrength = 0.2;
            vec3 ambient = uAmbientStrength * lightColor;

            //float diffuseStrength = 0.3;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * uDiffuseStrength;

            //float specularStrength = 0.5;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess) / max(dot(norm,viewDir), -dot(norm,lightDir));
            vec3 specular = uSpecularStrength * spec * lightColor;  
            
            // texture color
            vec4 textureColor = texture(uTexture, outTexture);

            vec3 result = (ambient + diffuse + specular) * (outCol.rgb + textureColor.rgb);
            FragColor = vec4(result, outCol.w);
            //FragColor = texture(uTexture, outTexture);
        }