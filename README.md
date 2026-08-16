# ParkMinPackages.PackageManager

ParkMinPackages 조직의 Unity 패키지를 조회하고 관리하는 Editor 도구입니다.

## Dependency metadata

- Unity Registry 의존성은 각 패키지의 `package.json`에 작성합니다.
- Git 및 NuGet 의존성은 각 저장소 루트의 `parkmin-dependencies.json`에 작성합니다.
- PackageManager는 Git 및 NuGet 의존성의 설치 상태를 조회해 한 줄씩 표시하지만 자동으로 설치하지 않습니다.
- `Show Dependencies` 설정으로 의존성 표시 여부를 변경할 수 있습니다.

```json
{
  "schemaVersion": 1,
  "gitDependencies": [
    {
      "packageName": "com.parkminpackages.foundation",
      "url": "https://github.com/ParkMinPackages/Foundation.git"
    }
  ],
  "nugetDependencies": [
    {
      "packageName": "Example.Package",
      "version": "1.0.0"
    }
  ]
}
```
