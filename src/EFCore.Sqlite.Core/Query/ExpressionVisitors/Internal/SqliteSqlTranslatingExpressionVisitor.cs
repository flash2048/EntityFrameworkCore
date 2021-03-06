﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Sqlite.Query.ExpressionVisitors.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class SqliteSqlTranslatingExpressionVisitor : SqlTranslatingExpressionVisitor
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public SqliteSqlTranslatingExpressionVisitor(
            [NotNull] SqlTranslatingExpressionVisitorDependencies dependencies,
            [NotNull] RelationalQueryModelVisitor queryModelVisitor,
            [NotNull] SelectExpression targetSelectExpression = null,
            [NotNull] Expression topLevelPredicate = null,
            bool inProjection = false)
            : base(dependencies, queryModelVisitor, targetSelectExpression, topLevelPredicate, inProjection)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            var visitedExpression = base.VisitBinary(binaryExpression);

            if (visitedExpression == null)
            {
                return null;
            }

            switch (visitedExpression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                    return IsDateTimeBasedOperation(visitedExpression)
                        ? null
                        : visitedExpression;
            }

            return visitedExpression;
        }

        private static bool IsDateTimeBasedOperation(Expression expression)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                var typeMapping = InferTypeMappingFromColumn(binaryExpression.Left)
                    ?? InferTypeMappingFromColumn(binaryExpression.Right);

                if (typeMapping != null
                    && (typeMapping is SqliteDateTimeOffsetTypeMapping
                        || typeMapping is SqliteDateTimeTypeMapping))
                {
                    return true;
                }
            }

            return false;
        }

        private static RelationalTypeMapping InferTypeMappingFromColumn(Expression expression)
            => expression.FindProperty(expression.Type)?.FindRelationalMapping();

    }
}
