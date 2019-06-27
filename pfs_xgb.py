import numpy as np
import pandas as pd
import xgboost as xgb
import sklearn.preprocessing as skp
import sklearn.compose as skc

from config import *
from data_analysis import *

def make_simple_matrix_shop_item(use_categories = False, use_date_block_num = False, exclude_last_month = False)\
->(xgb.DMatrix, skc.ColumnTransformer):
    tr_grouped = pd.read_hdf(datadir + 'item_shop_month_sales.h5')
    if exclude_last_month:
        tr_grouped = tr_grouped[tr_grouped.date_block_num != 33]
    labels = tr_grouped['item_cnt_month'].tolist()
    one_hot_features = ['shop_id', 'item_id']
    if use_categories:
        one_hot_features.append('item_category_id')
    all_features = one_hot_features.copy()
    if use_date_block_num:
        all_features.append('date_block_num')
    tr_grouped = tr_grouped[all_features]
    column_transformer = skc.make_column_transformer((skp.OneHotEncoder(categories='auto'),\
                                                      one_hot_features),\
                                                     n_jobs=4, remainder='passthrough')
    tr_sparse = column_transformer.fit_transform(tr_grouped)
    del tr_grouped
    feature_names = column_transformer.named_transformers_['onehotencoder'].get_feature_names(one_hot_features)
    if use_date_block_num:
        feature_names = np.append(feature_names, ['date_block_num'])
    return (xgb.DMatrix(tr_sparse, label=labels, feature_names=feature_names, nthread=4), column_transformer)

def make_eval_matrix_shop_item(fit_column_transformer, use_categories = False, use_date_block_num = False)\
-> xgb.DMatrix:
    tr_grouped = pd.read_hdf(datadir + 'item_shop_month_sales.h5')
    tr_grouped = tr_grouped[tr_grouped.date_block_num == 33]
    labels = tr_grouped['item_cnt_month'].tolist()
    one_hot_features = ['shop_id', 'item_id']
    if use_categories:
        one_hot_features.append('item_category_id')
    all_features = one_hot_features.copy()
    if use_date_block_num:
        all_features.append('date_block_num')
    tr_grouped = tr_grouped[all_features]
    tr_sparse = fit_column_transformer.transform(tr_grouped)
    del tr_grouped
    feature_names = fit_column_transformer.named_transformers_['onehotencoder'].get_feature_names(one_hot_features)
    if use_date_block_num:
        feature_names = np.append(feature_names, ['date_block_num'])
    return xgb.DMatrix(tr_sparse, label=labels, feature_names=feature_names, nthread=4)

def make_simple_dtest_shop_item(\
fit_column_transformer, use_categories = False, use_date_block_num = False)\
-> xgb.DMatrix:
    one_hot_features = ['shop_id', 'item_id']
    test_set = pd.read_csv(comp_data + 'test.csv').drop('ID', axis=1)
    if use_categories:
        one_hot_features.append('item_category_id')
        items = pd.read_csv(comp_data + 'items.csv')[['item_id', 'item_category_id']]
        test_set = test_set.merge(items, how='left', on = ['item_id'])
    if use_date_block_num:
        test_set['date_block_num'] = np.full((test_set.shape[0]), 34)
    ts_sparse = fit_column_transformer.transform(test_set)
    feature_names =\
        fit_column_transformer.named_transformers_['onehotencoder'].get_feature_names(one_hot_features)
    if use_date_block_num:
        feature_names = np.append(feature_names, ['date_block_num'])
    return xgb.DMatrix(ts_sparse, feature_names=feature_names, nthread=4)

def make_predictions_shop_item(\
prediction_filename, param, num_boost_round,\
use_categories = False, use_date_block_num = False, use_evaluation = False)\
-> xgb.Booster:
    dtrain, column_transformer =\
        make_simple_matrix_shop_item(use_categories, use_date_block_num, exclude_last_month = use_evaluation)
    deval = make_eval_matrix_shop_item(column_transformer, use_categories, use_date_block_num) if use_evaluation else None
    booster = train_model(param, num_boost_round, None, dtrain, deval)
    del dtrain
    del deval
    dtest = make_simple_dtest_shop_item(column_transformer, use_categories, use_date_block_num)
    predictions = booster.predict(dtest)
    test_set = pd.read_csv(comp_data + 'test.csv')
    test_set['item_cnt_month'] = predictions
    # clip sales to max 20
    test_set['item_cnt_month'] = test_set['item_cnt_month'].apply(lambda x: np.minimum(x, 20))
    test_set[['ID', 'item_cnt_month']].to_csv(prediction_filename, index=False)
    return booster

#################################################################################

def train_model(param, num_boost_round, early_stopping_rounds, dtrain, deval = []) -> xgb.Booster:
    watchlist = [(dtrain,'train')]
    evalnum = 1;
    for eval_matrix in deval:
        watchlist.append((eval_matrix,'eval' + str(evalnum)))
        evalnum += 1
    evals_result = dict()
    return xgb.train(param, dtrain, num_boost_round, evals = watchlist, evals_result= evals_result,\
                        verbose_eval = True, early_stopping_rounds = early_stopping_rounds)

def make_train_matrix(validation_months = [])\
->(xgb.DMatrix, skc.ColumnTransformer):
    sales_train = get_train_data()
    if len(validation_months) > 0:
        print("Excluding validation months from train")
        sales_train = sales_train[~(sales_train.date_block_num.isin(validation_months))]
    print("Train rows: ", sales_train.shape[0])
    labels = sales_train['item_cnt_month'].tolist()
    one_hot_features = ['month_of_year', 'city_code']#['shop_id', 'item_id', 'item_category_id', 'month_of_year']
    all_features = one_hot_features.copy()
    all_features = all_features + ['date_block_num', 'item_cnt_month_lag_1']\
                                + ['shop_id', 'item_id', 'item_category_id']
    print('Used features: ' + str(all_features))
    sales_train = sales_train[all_features]
    column_transformer = skc.make_column_transformer((skp.OneHotEncoder(categories='auto'),\
                                                      one_hot_features),\
                                                     n_jobs=1, remainder='passthrough')
    sales_train_sparse = column_transformer.fit_transform(sales_train)
    del sales_train
    feature_names = column_transformer.named_transformers_['onehotencoder'].get_feature_names(one_hot_features)
    feature_names = np.concatenate((feature_names, ['date_block_num', 'item_cnt_month_lag_1'], ['shop_id', 'item_id', 'item_category_id']))
    return (xgb.DMatrix(sales_train_sparse, label=labels, feature_names=feature_names, nthread=4), column_transformer)

def make_eval_matrix(fit_column_transformer, month_num = 33) -> xgb.DMatrix:
    sales_eval = get_train_data()
    sales_eval = sales_eval[sales_eval.date_block_num == month_num]
    print("Adding eval matrix with row count: ", sales_eval.shape[0])
    labels = sales_eval['item_cnt_month'].tolist()
    one_hot_features = ['month_of_year', 'city_code']#['shop_id', 'item_id', 'item_category_id', 'month_of_year']
    all_features = one_hot_features.copy()
    all_features = all_features + ['date_block_num', 'item_cnt_month_lag_1']\
                                + ['shop_id', 'item_id', 'item_category_id']
    sales_eval = sales_eval[all_features]
    sales_eval_sparse = fit_column_transformer.transform(sales_eval)
    del sales_eval
    feature_names = fit_column_transformer.named_transformers_['onehotencoder'].get_feature_names(one_hot_features)
    feature_names = np.concatenate((feature_names, ['date_block_num', 'item_cnt_month_lag_1'], ['shop_id', 'item_id', 'item_category_id']))
    return xgb.DMatrix(sales_eval_sparse, label=labels, feature_names=feature_names, nthread=4)

def make_dtest(fit_column_transformer) -> xgb.DMatrix:
    one_hot_features = ['month_of_year', 'city_code']#['shop_id', 'item_id', 'item_category_id', 'month_of_year']
    all_features = one_hot_features.copy()
    all_features = all_features + ['date_block_num', 'item_cnt_month_lag_1']\
                                + ['shop_id', 'item_id', 'item_category_id']
    test_set = get_test_data().drop('ID', axis=1)
    items = pd.read_csv(comp_data + 'items.csv')[['item_id', 'item_category_id']]
    test_set = test_set.merge(items, how='left', on = ['item_id'])
    test_set['date_block_num'] = np.full((test_set.shape[0]), 34)
    test_set['month_of_year'] = np.full((test_set.shape[0]), 10)
    
    sales_train = get_train_data().query('date_block_num == 33')\
                                  [['shop_id', 'item_id', 'item_cnt_month']]\
                                  .rename(columns={'item_cnt_month': 'item_cnt_month_lag_1'})
    test_set = test_set.merge(sales_train, on=['shop_id', 'item_id'], how='left')
    
    test_set = test_set[all_features]
    ts_sparse = fit_column_transformer.transform(test_set)
    feature_names =\
        fit_column_transformer.named_transformers_['onehotencoder'].get_feature_names(one_hot_features)
    feature_names = np.concatenate((feature_names, ['date_block_num', 'item_cnt_month_lag_1'], ['shop_id', 'item_id', 'item_category_id']))
    return xgb.DMatrix(ts_sparse, feature_names=feature_names, nthread=4)

def make_predictions(prediction_filename, param, num_boost_round, early_stopping_rounds, validation_months = []) -> (xgb.Booster, pd.DataFrame):
    dtrain, column_transformer = make_train_matrix(validation_months)
    deval = []
    for validation_month in validation_months:
        deval.append(make_eval_matrix(column_transformer, validation_month))
    booster = train_model(param, num_boost_round, early_stopping_rounds, dtrain, deval)
    del dtrain
    del deval
    dtest = make_dtest(column_transformer)
    predictions = booster.predict(dtest)
    test_set = get_test_data()
    test_set['item_cnt_month'] = predictions
    test_set.item_cnt_month = test_set.item_cnt_month.clip(0, 20)
    #test_set = fix_unknown_samples(test_set)
    test_set[['ID', 'item_cnt_month']].to_csv('preds/' + prediction_filename, index=False)
    return booster, test_set
    
def plot_importance(booster) -> None:
    ax = xgb.plot_importance(booster, max_num_features=50)
    fig = ax.figure
    fig.set_size_inches(15, 30)

#param = {'max_depth':7, 'eta':0.15, 'verbosity':1, 'objective':'reg:linear', 'eval_metric':'rmse'}
#booster2 = pfs_xgb.make_predictions_shop_item('pred11_xgb.csv', param, num_boost_round = 500, use_categories = True,\
#                                     use_date_block_num = True, use_evaluation = True)