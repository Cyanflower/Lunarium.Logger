#!/bin/bash

set -e

HTML=false
for arg in "$@"; do
  [[ "$arg" == "--html" ]] && HTML=true
done

echo "🧹 清空旧数据..."
rm -rf ./TestResults/*

echo "🧪 运行测试并收集覆盖率..."
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

echo "📊 生成覆盖率报告..."
if [ "$HTML" = true ]; then
  rm -rf ./CoverageReport/html/*
  reportgenerator \
    -reports:"./TestResults/**/coverage.cobertura.xml" \
    -targetdir:"./CoverageReport/html" \
    -reporttypes:Html
  echo ""
  echo "✅ 完成！HTML 报告位于 ./CoverageReport/html/index.html"
else
  rm -f ./CoverageReport/Summary.txt
  reportgenerator \
    -reports:"./TestResults/**/coverage.cobertura.xml" \
    -targetdir:"./CoverageReport" \
    -reporttypes:TextSummary
  echo ""
  echo "✅ 完成！报告位于 ./CoverageReport/"
  echo "   汇总：./CoverageReport/Summary.txt"
fi
